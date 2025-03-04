using System.Windows;
using System.Windows.Controls;

namespace ClientYT
{
	public partial class MainWindow : Window
	{
		private readonly YouTubeDownloaderService _youTubeDownloaderService;
		private readonly List<(string Url, string Format, string OutputPath, string Title)> _downloadQueue;
		private bool _isDownloading;

		public MainWindow()
		{
			InitializeComponent();
			string ffmpegPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
			_youTubeDownloaderService = new YouTubeDownloaderService(ffmpegPath);
			_downloadQueue = new List<(string Url, string Format, string OutputPath, string Title)>();
			_isDownloading = false;
		}
		private async void btnAddToQueue_Click(object sender, RoutedEventArgs e)
		{
			string url = txtUrl.Text.Trim();
			string selectedFormat = ((ComboBoxItem)cmbFormat.SelectedItem)?.Content.ToString() ?? "MP4";

			if (string.IsNullOrEmpty(url) || url == "Enter YouTube URL")
			{
				lblStatus.Content = "Error: Invalid URL";
				MessageBox.Show("Please enter a valid YouTube URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			lblStatus.Content = "Validating URL ...";
			var (isValid, errorMessage) = await _youTubeDownloaderService.IsValidYouTubeUrl(url);
			if (!isValid)
			{
				lblStatus.Content = "Error: Invalid URL";
				MessageBox.Show($"Invalid YouTube URL: {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			lblStatus.Content = "Fetching video information ...";
			var video = await _youTubeDownloaderService.GetVideoTitleAsync(url);
			string videoTitle = video.Title.Length > 50 ? video.Title.Substring(0, 47) + "..." : video.Title;

			var saveFileDialog = new Microsoft.Win32.SaveFileDialog
			{
				Title = $"Save {selectedFormat.ToUpper()} File As",
				Filter = $"{selectedFormat.ToUpper()} Files (*.{selectedFormat.ToLower()})|*.{selectedFormat.ToLower()}",
				DefaultExt = $".{selectedFormat.ToLower()}",
				FileName = $"{(videoTitle)}.{selectedFormat.ToLower()}"
			};

			if (saveFileDialog.ShowDialog() != true)
			{
				lblStatus.Content = "Download cancelled";
				return;
			}

			string outPath = saveFileDialog.FileName;
			_downloadQueue.Add((url, selectedFormat, outPath, videoTitle));
			lstQueue.Items.Add($"{videoTitle} ({selectedFormat})");
			lblStatus.Content = $"Added to download queue ({_downloadQueue.Count} items)";
			txtUrl.Text = "Enter YouTube URL"; // Reset to placeholder text
		}
		private async void btnDownload_Click(object sender, RoutedEventArgs e)
		{

			if (_downloadQueue.Count == 0)
			{
				lblStatus.Content = "Error: Queue is empty";
				MessageBox.Show("Please add items to the download queue first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (_isDownloading)
			{
				lblStatus.Content = "Error: Download in progress";
				return;
			}

			_isDownloading = true;
			progressBar.Value = 0;
			progressBar.Visibility = Visibility.Visible;
			btnDownload.IsEnabled = false;
			btnDownload.Content = "Downloading...";
			btnClear.IsEnabled = false;
			btnAddToQueue.IsEnabled = false;

			bool allSuccessful = true;
			int totalItems = _downloadQueue.Count;


			while (_downloadQueue.Any())
			{
				var (url, format, outPath, title) = _downloadQueue[0];
				lblStatus.Content = $"Downloading {totalItems - _downloadQueue.Count + 1}/{totalItems}: {title} ({format})...";

				try
				{
					if (format == "MP4")
					{
						await _youTubeDownloaderService.DownloadMp4Async(url, outPath,
							percent => Dispatcher.Invoke(() =>
							{
								progressBar.Value = percent;
								lblStatus.Content = percent < 50 ? $"Downloading Video {totalItems - _downloadQueue.Count + 1}/{totalItems}..." : $"Merging Streams {totalItems - _downloadQueue.Count + 1}/{totalItems}...";
							}));
					}
					else if (format == "MP3")
					{
						await _youTubeDownloaderService.DownloadMp3Async(url, outPath,
							percent => Dispatcher.Invoke(() =>
							{
								progressBar.Value = percent;
								lblStatus.Content = $"Converting to MP3 {totalItems - _downloadQueue.Count + 1}/{totalItems}..."; // MP3 conversion status
							}));
					}

					lblStatus.Content = $"Completed {totalItems - _downloadQueue.Count}/{totalItems}: {title}";
				}
				catch (System.IO.IOException ex)
				{
					lblStatus.Content = "Network Error";
					MessageBox.Show(
						$"Network error: {ex.Message}. Check your internet connection or try a different URL.",
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Error
						);
					allSuccessful = false;
				}
				catch (Exception ex)
				{
					lblStatus.Content = "Error";
					MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					allSuccessful = false;
				}

				_downloadQueue.RemoveAt(0);
				lstQueue.Items.RemoveAt(0);

			}
			if (allSuccessful && totalItems > 0)
			{
				MessageBox.Show("All downloads completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			}


			_isDownloading = false;
			progressBar.Visibility = Visibility.Hidden;
			btnDownload.IsEnabled = true;
			btnDownload.Content = "Download Queue";
			btnClear.IsEnabled = true;
			btnAddToQueue.IsEnabled = true;
			lblStatus.Content = "All downloads completed";

		}
		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (lstQueue.SelectedIndex != -1) // -1 means no item is selected
				{
					int selectedIndex = lstQueue.SelectedIndex;
					var (url, format, outPath, title) = _downloadQueue[selectedIndex];
					_downloadQueue.RemoveAt(selectedIndex);
					lstQueue.Items.RemoveAt(selectedIndex);
					lblStatus.Content = $"Deleted '{title}' from queue ({_downloadQueue.Count} items remaining)";

					if (_downloadQueue.Count == 0)
					{
						lblStatus.Content = "Ready";
					}
				}
				else
				{
					lblStatus.Content = "Error: No item selected to delete";
				}
			}
			catch (Exception ex)
			{
				lblStatus.Content = "Error";
				MessageBox.Show($"Error in Delete: {ex.Message}\nStackTrace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				txtUrl.Text = "Enter YouTube URL"; // Reset to placeholder text
				lblStatus.Content = "Ready";
			}
			catch (Exception ex)
			{
				lblStatus.Content = "Error";
				MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			
		}
		private void TxtUrl_GotFocus(object sender, RoutedEventArgs e)
		{
			if (txtUrl.Text == "Enter YouTube URL")
				txtUrl.Text = "";
		}

	}
}