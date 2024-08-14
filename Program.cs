using YoutubeExplode;
using YoutubeExplode.Common;
using NReco.VideoConverter;

namespace YouTubeDownloader
{
    class Program
    {
        static readonly Random Random = new Random();
        static readonly ConsoleColor[] Colors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));

        static async Task Main(string[] args)
        {
            string outputDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}//videofolder";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Console.WriteLine("Please enter playlist URL link:");
            string playlistUrl = Console.ReadLine();

            await DownloadPlaylist(playlistUrl, outputDirectory);
        }

        static async Task DownloadPlaylist(string playlistUrl, string outputDirectory)
        {
            var youtube = new YoutubeClient();
            var playlist = await youtube.Playlists.GetVideosAsync(playlistUrl);

            foreach (var video in playlist)
            {
                var videoUrl = $"https://www.youtube.com/watch?v={video.Id}";
                await DownloadYouTubeVideo(videoUrl, outputDirectory);
            }
        }

        static async Task DownloadYouTubeVideo(string videoUrl, string outputDirectory)
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(videoUrl);

            string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

            if (muxedStreams.Any())
            {
                var streamInfo = muxedStreams.First();
                using var httpClient = new HttpClient();
                using var stream = await httpClient.GetStreamAsync(streamInfo.Url);

                string outputFilePath = Path.Combine(outputDirectory, $"{sanitizedTitle}.{streamInfo.Container}");
                using (var outputStream = File.Create(outputFilePath))
                {
                    await stream.CopyToAsync(outputStream);
                }

                Console.ForegroundColor = GetRandomConsoleColor();
                Console.WriteLine("Download completed!");
                Console.WriteLine($"Video saved as: {outputFilePath}");
                Console.ResetColor();

                // Convert the downloaded video file to MP3
                //string mp3OutputPath = Path.Combine(outputDirectory, $"{sanitizedTitle}.mp3");
                //ConvertToMp3(outputFilePath, mp3OutputPath);

                // Optionally, delete the original video file after conversion
               // File.Delete(outputFilePath);

                Console.ForegroundColor = GetRandomConsoleColor();
                //Console.WriteLine($"Video converted to MP3: {mp3OutputPath}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"No suitable video stream found for {video.Title}.");
                Console.ResetColor();
            }
        }

        static void ConvertToMp3(string inputFilePath, string outputFilePath)
        {
            try
            {
                var converter = new FFMpegConverter();
                converter.ConvertMedia(inputFilePath, outputFilePath, "mp3");
            }
            catch (FFMpegException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error converting video to MP3: {ex.Message}");
                Console.ResetColor();
            }
        }

        static ConsoleColor GetRandomConsoleColor()
        {
            ConsoleColor color;
            do
            {
                color = Colors[Random.Next(Colors.Length)];
            } while (color == Console.BackgroundColor);
            return color;
        }
    }
}
