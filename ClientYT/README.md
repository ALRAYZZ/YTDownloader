# YouTube Downloader

A simple WPF application to download YouTube videos and audio (MP4/MP3) with a queue system.

## Features
- Add YouTube URLs to a download queue.
- Display video titles in the queue.
- Delete specific items from the queue.
- Clear the URL field or remove selected items.
- Download videos in MP4 or MP3 format.

## Prerequisites
- **.NET 9.0 Desktop Runtime**: Download and install from [Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0).
- Windows OS (since the app targets `net9.0-windows`).

## Download and Run
1. Go to the [Releases](https://github.com/ALRAYZZ/YTDownloader/releases/tag/v1.0.0) page.
2. Download the latest release ZIP file (e.g., `YouTubeDownloaderRelease.zip`).
3. Extract the ZIP to a folder on your PC.
4. Double-click `ClientYT.exe` to run the app.

## Notes
- The `ffmpeg` folder must remain in the same directory as `ClientYT.exe`.
- Save downloaded files to a location of your choice when prompted.

## Troubleshooting
- If you see an error about missing .NET, install the .NET 9.0 Desktop Runtime.
- If downloads fail, check your internet connection.
- If FFmpeg errors occur, ensure `ffmpeg.exe` is in the `ffmpeg` folder.

## Building from Source
If you wish to build the app yourself:
1. Clone this repository: `git clone https://github.com/ALRAYZZ/YTDownloader`
2. Open `ClientYT.sln` in Visual Studio.
3. Restore NuGet packages (e.g., `YoutubeExplode`, `Xabe.FFmpeg`).
4. Place `ffmpeg.exe` in the `ffmpeg` folder in the project root.
5. Build in Release mode and run.