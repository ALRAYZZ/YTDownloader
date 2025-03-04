using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Xabe.FFmpeg;
using System.IO;
using System.Diagnostics;
using System.Windows;
using YoutubeExplode.Videos;

public class YouTubeDownloaderService
{
	private readonly YoutubeClient _youtubeClient;
	private readonly string _ffmpegPath;

	public YouTubeDownloaderService(string? ffmpegPath = null)
	{
		_youtubeClient = new YoutubeClient();
		_ffmpegPath = ffmpegPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
		if (!Directory.Exists(_ffmpegPath) || !File.Exists(Path.Combine(_ffmpegPath, "ffmpeg.exe")))
		{
			_ffmpegPath = AppDomain.CurrentDomain.BaseDirectory;
		}
		FFmpeg.SetExecutablesPath(_ffmpegPath);
		string fullPath = Path.Combine(_ffmpegPath, "ffmpeg.exe");
	}
	private void EnsureFFmpegExists()
	{
		string fullPath = Path.Combine(_ffmpegPath, "ffmpeg.exe");
		if (!File.Exists(fullPath))
			throw new InvalidOperationException($"Cannot find FFmpeg at: {fullPath}");
	}
	public async Task<(bool IsValid, string? ErrorMessage)> IsValidYouTubeUrl(string url)
	{
		try
		{
			await _youtubeClient.Videos.GetAsync(url);
			return (true, null);
		}
		catch (ArgumentException)
		{
			return (false, "Invalid URL format.");
		}
		catch (Exception ex)
		{
			return (false, $"Failed to validate URL: {ex.Message}");
		}
	}
	public async Task DownloadMp4Async(string url, string outputPath, Action<double>? onProgressUpdated = null)
	{
		var video = await _youtubeClient.Videos.GetAsync(url);
		var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
		string safeFileName = Path.Combine(Path.GetDirectoryName(outputPath) ?? "", $"{SanitizeFileName(video.Title)}.mp4");

		var muxedStreams = streamManifest.GetMuxedStreams();
		if (muxedStreams.Any())
		{
			var streamInfo = muxedStreams.GetWithHighestVideoQuality();
			await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, safeFileName,
				new Progress<double>(p => onProgressUpdated?.Invoke(p * 100)));
		}
		else
		{
			// Fallback to adaptive streams
			var videoStream = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
			var audioStream = streamManifest.GetAudioStreams().GetWithHighestBitrate();

			if (videoStream == null || audioStream == null)
				throw new InvalidOperationException($"No suitable video or audio streams found for: {url}");

			string videoTemp = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
			string audioTemp = Path.ChangeExtension(Path.GetTempFileName(), ".m4a");

			try
			{
				// Download video and audio separately
				await _youtubeClient.Videos.Streams.DownloadAsync(videoStream, videoTemp,
					new Progress<double>(p => onProgressUpdated?.Invoke(p * 40))); // 0-40% for video
				await _youtubeClient.Videos.Streams.DownloadAsync(audioStream, audioTemp);
					new Progress<double>(p => onProgressUpdated?.Invoke(40 + (p * 10))); // 40-50% for audio

				EnsureFFmpegExists();
				FFmpeg.SetExecutablesPath(_ffmpegPath);
				// Combine with FFmpeg
				var conversion = FFmpeg.Conversions.New()
					.AddParameter($"-i \"{videoTemp}\" -i \"{audioTemp}\" -c:v copy -c:a aac")
					.SetOutput(safeFileName)
					.SetOverwriteOutput(true);

				conversion.OnProgress += (sender, args) =>
				{
					double percent = 50 + (args.Percent / 2); // 50-100% for merging
					onProgressUpdated?.Invoke(percent);
				};

				await conversion.Start();
			}
			finally
			{
				if (File.Exists(videoTemp)) File.Delete(videoTemp);
				if (File.Exists(audioTemp)) File.Delete(audioTemp);
			}
		}
	}
	public async Task<string> DownloadAudioAsync(string url)
	{
		var video = await _youtubeClient.Videos.GetAsync(url);
		var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
		var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
		string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".m4a");
		await _youtubeClient.Videos.Streams.DownloadAsync(audioStream, tempFilePath);
		return tempFilePath;
	}
	public async Task ConvertToMp3Async(string inputPath, string outputPath, Action<double>? onProgressUpdated = null)
	{
		EnsureFFmpegExists();
		try
		{
			IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
			IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()
				?? throw new InvalidOperationException("No audio stream found in the file.");

			var conversion = FFmpeg.Conversions.New()
				.AddStream(audioStream)
				.SetOutput(outputPath)
				.SetOutputFormat(Format.mp3)
				.SetOverwriteOutput(true);

			conversion.OnProgress += (sender, args) =>
			{
				double percent = Math.Round((double)args.Percent, 2);
				onProgressUpdated?.Invoke(percent); // Already in 0-100 range
			};

			await conversion.Start();
		}
		catch (Exception ex)
		{
			throw new Exception($"Conversion failed: {ex.Message}", ex);
		}
	}
	public async Task DownloadMp3Async(string url, string outputPath, Action<double>? onProgressUpdated = null)
	{
		string tempFile = await DownloadAudioAsync(url);
		try
		{
			string safeFileName = Path.Combine(Path.GetDirectoryName(outputPath) ?? "",
				$"{SanitizeFileName((await _youtubeClient.Videos.GetAsync(url)).Title)}.mp3");
			await ConvertToMp3Async(tempFile, safeFileName, onProgressUpdated);
		}
		catch
		{
			if (File.Exists(tempFile)) File.Delete(tempFile);
			throw;
		}
	}
	public async Task<Video> GetVideoTitleAsync(string url)
	{
		try
		{
			return await _youtubeClient.Videos.GetAsync(url);
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to fetch video title: {ex.Message}", ex);
		}
	}
	private static string SanitizeFileName(string name) =>
		string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}
