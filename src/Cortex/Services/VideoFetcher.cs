using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Cortex.Services
{
    public interface IVideoFetcher
    {
        Task<IList<VideoInfo>> SearchVideosAsync(IList<string> keywords);
    }

    public class VideoFetcher : IVideoFetcher
    {
        private readonly ILogger<VideoFetcher> _logger;
        private readonly IConfiguration _config;

        public VideoFetcher(ILogger<VideoFetcher> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<IList<VideoInfo>> SearchVideosAsync(IList<string> keywords)
        {
            var apiKey = _config["QuickBoostAI:YouTubeApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // TODO: Implement YouTube Data API call using Google.Apis.YouTube.v3
                _logger.LogInformation("YouTube API key provided – would perform real search (not implemented). Returning empty list for now.");
                return new List<VideoInfo>();
            }
            else
            {
                // Fallback: return a static demo list
                _logger.LogInformation("YouTube API key not configured – using demo video list.");
                return new List<VideoInfo>
                {
                    new VideoInfo("dQw4w9WgXcQ", "Rick Astley - Never Gonna Give You Up", "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg"),
                    new VideoInfo("9bZkp7q19f0", "PSY - Gangnam Style", "https://www.youtube.com/watch?v=9bZkp7q19f0", "https://img.youtube.com/vi/9bZkp7q19f0/hqdefault.jpg")
                };
            }
        }
    }

    public record VideoInfo(string VideoId, string Title, string Url, string Thumbnail);
}
