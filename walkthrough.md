# Personalized Cognitive RAG & Multimodal Ingestion — Walkthrough

We have implemented a personalized **Retrieval-Augmented Generation (RAG)** search assistant integrated with a **User Mindset** profiling engine. The application now learns from the user's reading trajectory, saved items, and queries, and tailors all AI responses to match their specific learning profile.

Additionally, we built a **Multimodal Video Processing Pipeline** that downloads low-resolution video tracks (e.g. YouTube, Instagram Reels, TikTok, and direct video files), extracts both the audio track and keyframe frames dynamically, analyzes them using Google Gemini multimodal vision, and stores a unified transcript/visual timeline. We also replaced blind context truncation in RAG with a **Smart Keyword-Scored Sliding Window Chunk Retriever**.

---

## What Was Added

### 1. Database Schema (`UserMindsets`)
- **Table**: Created `UserMindsets` table mapping 1:1 with `Users` (cascading deletes).
- EF Core migrations were generated and applied automatically at server startup.

### 2. User Mindset Profiler (ML & Personalization)
- **Service**: Implemented `UserMindsetService`.
- **Logic**: Loads recent saved items and activity logs to compile a personalized JSON profile.
- **Trigger**: Compiles automatically on a user's first RAG query, or whenever they click the "AI Profile Recalculate" button.
- **Manual Overrides**: Includes support for editing/tweaking preferences manually.

### 3. Multi-Mode RAG Query Engine
- **Endpoint**: Added `POST /api/v1/search/rag`.
- **Query Modes**: Hybrid, Vector, Keyword (Vectorless), and SelectedLinks.
- **Cognitive Prompts**: Injects the active mindset profile and consumption preferences as system guidelines.

### 4. Multimodal Video Processing Pipeline
- **Low-Resolution Video Ingestion**: Updated `LocalVideoExtractor.cs` to download video tracks using `yt-dlp` flag `-f "worstvideo[height<=240]+worstaudio/worst/worst"` and merge to MP4. Prepend `/usr/local/bin` to the child process environment `PATH` variable to cleanly resolve shebang Python versions and binary executions.
- **Audio-Visual Splitting**: Used `ffmpeg` to:
  - Extract the audio track as a 16kHz mono WAV file for local Whisper transcription.
  - Extract keyframes as scaled JPEGs (width 480px) at dynamic intervals based on video duration (10s intervals for <= 2 min videos, 30s intervals for <= 10 min videos, and 60s intervals for longer videos).
- **Vision Timeline Synthesis**: Added `DescribeVideoFramesAsync` to `AIExtractionService`. It formats sampled keyframe base64 images into a multimodal OpenAI-compatible schema and prompts Gemini to describe the visual progression chronological timeline (e.g. slide topics, on-screen code snippets, demonstrated UIs).
- **Knowledgebase Formulation**: Combines the visual description timeline and audio transcript under separate markdown sections inside `RawText` (`[Visual Timeline & Keyframe Description]` and `[Audio Transcript]`) for all video content processors.
- **Automatic Cleanup**: Deletes all temporary MP4 and JPEG frame files after encoding.

### 5. Smart RAG Context Retrieval
- **Problem**: Long video transcripts or articles exceeded LLM token limits and were blindly truncated to the first 5000 characters (cutting off high-relevance parts).
- **Solution**: Implemented a keyword-scored sliding window retriever in `RAGQueryUseCase.cs`. It splits content into overlapping sentences, scores them based on overlap with query keywords, and extracts the top 5 highest-relevance chunks (up to 4000 characters total) joined by ellipses, falling back to initial text only if no keywords match.

---

## Verification Results

1. **Build Integrity**: 100% build success (0 errors, 1 warning).
2. **Path Resolution**: Confirmed child processes prepending `/usr/local/bin` resolves `yt-dlp` execution under Python 3.11 perfectly.
3. **Multimodal Extraction**: Verified low-res MP4 downloaded, WAV extracted, keyframes sampled, base64-encoded, and described by Gemini.
4. **Smart Chunking**: Verified that keyword matching extracts the correct context slices for the query, preserving full-transcript search accuracy.
