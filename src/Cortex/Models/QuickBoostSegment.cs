using System;

namespace Refind.Cortex.Models
{
    public class QuickBoostSegment
    {
        public Guid VideoId { get; set; }
        public string VideoUrl { get; set; } = string.Empty; // full URL to video file
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
