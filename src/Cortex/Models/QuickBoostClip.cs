using System;

namespace Cortex.Models
{
    /// <summary>
    /// Represents a short video clip for the QuickBoost feature.
    /// </summary>
    public record QuickBoostClip(
        string VideoUrl,
        double StartSec,
        double EndSec,
        string Title,
        string Thumbnail,
        string Emotion);
}
