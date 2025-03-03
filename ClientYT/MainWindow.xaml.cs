using System.Windows;
using System.Windows.Controls;

namespace ClientYT
{
	public partial class MainWindow : Window
	{
		private readonly YouTubeDownloaderService _youTubeDownloaderService;
		private readonly List<(string Url, string Format, string OutputPath)> _downloadQueue;
		private bool _isDownloading;

		public MainWindow()
		{
			InitializeComponent();
			string ffmpegPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
			_youTubeDownloaderService = new YouTubeDownloaderService(ffmpegPath);
			_downloadQueue = new List<(string Url, string Format, string OutputPath)>();
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

			var saveFileDialog = new Microsoft.Win32.SaveFileDialog
			{
				Title = $"Save {selectedFormat.ToUpper()} File As",
				Filter = $"{selectedFormat.ToUpper()} Files (*.{selectedFormat.ToLower()})|*.{selectedFormat.ToLower()}",
				DefaultExt = $".{selectedFormat.ToLower()}"
			};

			if (saveFileDialog.ShowDialog() != true)
			{
				lblStatus.Content = "Download cancelled";
				return;
			}

			string outPath = saveFileDialog.FileName;
			_downloadQueue.Add((url, selectedFormat, outPath));
			lstQueue.Items.Add($"{url} ({selectedFormat})");
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
				var (url, format, outPath) = _downloadQueue[0];
				lblStatus.Content = $"Downloading: {url} ({format})...";

				try
				{
					if (format == "MP4")
					{
						await _youTubeDownloaderService.DownloadMp4Async(url, outPath,
							percent => Dispatcher.Invoke(() =>
							{
								progressBar.Value = percent;
								lblStatus.Content = percent < 50 ? "Downloading Video..." : "Merging Streams...";
							}));
					}
					else if (format == "MP3")
					{
						await _youTubeDownloaderService.DownloadMp3Async(url, outPath,
							percent => Dispatcher.Invoke(() =>
							{
								progressBar.Value = percent;
								lblStatus.Content = "Converting to MP3..."; // MP3 conversion status
							}));
					}

					lblStatus.Content = "Download completed successfully";
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