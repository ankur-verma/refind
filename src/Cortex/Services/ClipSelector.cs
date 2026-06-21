using Cortex.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Cortex.Services
{
    public record ClipSelection(VideoInfo Video, EmotionSegment Emotion, QuickBoostClip Clip);

    public interface IClipSelector
    {
        Task<IList<QuickBoostClip>> SelectClipsAsync(IList<VideoInfo> videos, IList<EmotionSegment> emotions, IList<string> preferredEmotions);
    }

    public class ClipSelector : IClipSelector
    {
        private readonly ILogger<ClipSelector> _logger;
        private readonly IConfiguration _config;

        public ClipSelector(ILogger<ClipSelector> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<IList<QuickBoostClip>> SelectClipsAsync(IList<VideoInfo> videos, IList<EmotionSegment> emotions, IList<string> preferredEmotions)
        {
            var result = new List<QuickBoostClip>();
            if (!videos.Any() || !emotions.Any())
            {
                _logger.LogWarning("No videos or emotions available for clip selection.");
                return result;
            }

            // Filter emotions by preferred list (case‑insensitive)
            var filtered = emotions
                .Where(e => preferredEmotions.Contains(e.Emotion, System.StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (!filtered.Any())
            {
                // If none match, fall back to all emotions
                filtered = emotions.ToList();
            }

            // Simple strategy: assign each filtered emotion to the first video (or rotate if many)
            var videoCount = videos.Count;
            for (int i = 0; i < filtered.Count; i++)
            {
                var emo = filtered[i];
                var video = videos[i % videoCount];
                var clip = new QuickBoostClip(
                    VideoUrl: $"https://www.youtube.com/embed/{video.VideoId}",
                    StartSec: emo.StartSec,
                    EndSec: emo.EndSec,
                    Title: video.Title,
                    Thumbnail: video.Thumbnail,
                    Emotion: emo.Emotion);
                result.Add(clip);
            }

            _logger.LogInformation("Selected {Count} clips for QuickBoost.", result.Count);
            await Task.CompletedTask;
            return result;
        }
    }
}
