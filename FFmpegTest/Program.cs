using System.Diagnostics;

public class Program
{
	public static void Main(string[] args)
	{
		string ffmpegPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe"));



		Console.WriteLine($"Looking for FFmpeg at: {ffmpegPath}");
		Console.WriteLine($"File exists: {File.Exists(ffmpegPath)}");
		if (!File.Exists(ffmpegPath))
		{
			Console.WriteLine("FFmpeg executable not found.");
			return;
		}

		try
		{
			Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = ffmpegPath,
					Arguments = "-version", // Simple command to check FFmpeg version
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			Console.WriteLine("FFmpeg executed successfully:");
			Console.WriteLine(output);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to execute FFmpeg: {ex.Message}");
		}
	}
}