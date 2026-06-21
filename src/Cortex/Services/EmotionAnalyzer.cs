using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Cortex.Services
{
    public record EmotionSegment(string Emotion, double StartSec, double EndSec);

    public interface IEmotionAnalyzer
    {
        Task<IList<EmotionSegment>> AnalyzeAsync(string transcript);
    }

    public class EmotionAnalyzer : IEmotionAnalyzer
    {
        private readonly ILogger<EmotionAnalyzer> _logger;
        private readonly IConfiguration _config;

        public EmotionAnalyzer(ILogger<EmotionAnalyzer> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<IList<EmotionSegment>> AnalyzeAsync(string transcript)
        {
            // Simple keyword‑based mock analysis. In a real implementation you would call Gemini sentiment API.
            var segments = new List<EmotionSegment>();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return segments;
            }

            var lower = transcript.ToLowerInvariant();
            // Mock timestamps – assume each keyword appears at 10s intervals.
            double cursor = 0;
            foreach (var word in new[] { "happy", "inspirational", "calm", "excited" })
            {
                if (lower.Contains(word))
                {
                    segments.Add(new EmotionSegment(word, cursor, cursor + 30));
                    cursor += 30; // advance for next possible segment
                }
            }

            _logger.LogInformation("Emotion analysis produced {Count} segments.", segments.Count);
            await Task.CompletedTask;
            return segments;
        }
    }
}
