# Full Video Understanding via Gemini

This plan outlines the integration of Gemini's File API to analyze entire videos, matching how Gemini natively processes video URLs. This will replace or supplement the current approach of extracting audio (via Whisper) and keyframes locally.

## User Review Required

> [!IMPORTANT]
> The current system uses `LocalVideoExtractor` to download videos, extract audio for Whisper transcription, and extract keyframes for visual description.
> With Gemini's native video support, Gemini can process the raw MP4 file directly for both audio and visual understanding.
> 
> **Decision:** Should we completely bypass the local Whisper and keyframe extraction when the Gemini API is in use, or keep them as fallbacks? Bypassing them will be much faster locally and use less CPU.

## Open Questions

> [!WARNING]
> 1. Gemini's API endpoint configured in your settings `EndpointUrl` must be a native Gemini endpoint (e.g. `https://generativelanguage.googleapis.com/v1beta`) for the File API to work. Are you currently using a native Gemini API key?
> 2. What prompt would you like to use when asking Gemini to analyze the whole video? (e.g., "Provide a full transcript and a chronological visual description of this video.")

## Proposed Changes

---

### Infrastructure Layer

#### [MODIFY] [LocalVideoExtractor.cs](file:///Users/apple/Desktop/refind/refind/src/Cortex/Infrastructure/Media/LocalVideoExtractor.cs)
- Add a new method `DownloadVideoOnlyAsync(string url)` that uses `yt-dlp` to download the best or reasonable quality MP4 without extracting WAV or keyframes.
- Ensure the downloaded MP4 path is returned and properly cleaned up by the caller.

#### [MODIFY] [IAIExtractionService.cs](file:///Users/apple/Desktop/refind/refind/src/Cortex/Infrastructure/AI/IAIExtractionService.cs)
- Add a new method: `Task<string> AnalyzeVideoFileAsync(string mp4FilePath, CancellationToken ct = default);`

#### [MODIFY] [AIExtractionService.cs](file:///Users/apple/Desktop/refind/refind/src/Cortex/Infrastructure/AI/AIExtractionService.cs)
- Implement `AnalyzeVideoFileAsync`:
  1. Upload the MP4 file to `https://generativelanguage.googleapis.com/upload/v1beta/files?key=...`
  2. Poll the Gemini `files/{name}` endpoint until the file state is `ACTIVE`.
  3. Call the `models/{CompletionModel}:generateContent` endpoint using the `fileUri`.
  4. Prompt Gemini to extract a full visual timeline and transcript.
  5. Delete the file from Gemini using a `DELETE` request to clean up storage.

---

### Content Processing Layer

#### [MODIFY] [ContentProcessors.cs](file:///Users/apple/Desktop/refind/refind/src/Cortex/Modules/Content/Services/ContentProcessors.cs)
- Update `YouTubeContentProcessor`, `VideoContentProcessor`, `TikTokContentProcessor`, and `InstagramContentProcessor`.
- Check if the Gemini API is in use (e.g. via an injected setting or method).
- If using Gemini, download the MP4 using `DownloadVideoOnlyAsync`, pass the file path to `AnalyzeVideoFileAsync`, and use the returned analysis as the raw text.
- Maintain the HTML metadata parsing as a fallback.

## Verification Plan

### Manual Verification
- Share a YouTube or direct Video URL through the AI extraction worker queue.
- Verify that `yt-dlp` downloads the video to the temp directory.
- Verify that `AIExtractionService` successfully uploads it to Gemini.
- Verify that the Gemini `generateContent` API returns a rich understanding of the video.
- Verify that the temporary MP4 is deleted from both the local disk and Gemini's servers after processing.
