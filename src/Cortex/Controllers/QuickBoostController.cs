using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Refind.Cortex.Models;

namespace Refind.Cortex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickBoostController : ControllerBase
    {
        private const string MetadataPath = "App_Data/videoMetadata.json";

        private class VideoMetadata
        {
            public string VideoId { get; set; } = string.Empty;
            public string VideoUrl { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string[] Tags { get; set; } = new string[0];
            public double Duration { get; set; }
            public string ThumbnailUrl { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }

        [HttpGet]
        public ActionResult<IEnumerable<QuickBoostSegment>> Get([FromQuery] string? category)
        {
            if (!System.IO.File.Exists(MetadataPath))
            {
                return NotFound($"Metadata file not found at {MetadataPath}");
            }

            var json = System.IO.File.ReadAllText(MetadataPath);
            var videos = JsonSerializer.Deserialize<List<VideoMetadata>>(json) ?? new List<VideoMetadata>();

            var filtered = string.IsNullOrWhiteSpace(category)
                ? videos
                : videos.Where(v => v.Category.Equals(category, System.StringComparison.OrdinalIgnoreCase)).ToList();

            // Create short segments – use 0‑90 seconds (or video length if shorter)
            var maxClip = 90.0; // seconds
            var segments = filtered.Select(v => new QuickBoostSegment
            {
                VideoId = System.Guid.Parse(v.VideoId),
                VideoUrl = v.VideoUrl,
                Title = v.Title,
                ThumbnailUrl = v.ThumbnailUrl,
                Category = v.Category,
                StartSeconds = 0,
                EndSeconds = v.Duration < maxClip ? v.Duration : maxClip
            }).ToList();

            return Ok(segments);
        }
    }
}
