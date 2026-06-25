# Refind API Documentation

This document traces the application's REST APIs, mapping endpoints to the background use cases and database tables they affect.

## API to Database Traceability Matrix

| API Endpoint | Service/UseCase | Repository | Tables Read | Tables Written | LLM Usage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **POST /api/v1/content** | `SaveContentUseCase` | `ContentItemRepository` | `ContentItems` (Check) | `ContentItems` | No |
| **GET /api/v1/content/feed** | `GetContentFeedUseCase` | `ContentItemRepository` | `ContentItems`, `Tags` | None | No |
| **DELETE /api/v1/content/{id}** | `DeleteContentUseCase` | `ContentItemRepository` | `ContentItems` | `ContentItems` (Cascades) | No |
| **POST /api/v1/search/rag** | `RAGUseCase`, `SemanticSearch`| `ContentPayloadRepository` | `ContentPayloads`, `Items` | None | Yes (Embed, Chat) |
| **GET /api/v1/quickboost/watch-plan** | `SemanticSearchUseCase` | `ContentPayloadRepository` | `ContentPayloads`, `Segments`| None | Yes (Embed) |

## Background Processing Triggered by APIs

### 1. AIExtractionWorker (Hosted Service)
*   **Trigger:** Message published to RabbitMQ `ai_extraction_queue` via `POST /api/v1/content`.
*   **Execution Flow:** Scrapes the URL/downloads the transcript. Sends text to Gemini for summarization, action item extraction, and embeddings.
*   **Dependencies:** RabbitMQ, PostgreSQL, Gemini API.
*   **Failure Handling:** Dead-letter queue mechanism. If processing fails, exception is logged, message is `Nack`ed and pushed to `ai_extraction_dlx` for retry or debugging.

### 2. Python Video Service (`ai-service/video_service.py`)
*   **Trigger:** HTTP POST request from `.NET AIExtractionWorker`.
*   **Execution Flow:** Uses `google.generativeai` and PyTube/YoutubeTranscriptAPI to analyze chunks of the transcript against timestamps. Returns JSON payload to be inserted into `VideoSegments`.
