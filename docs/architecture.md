# Refind Architecture

The application relies on a React/Vite Single Page Application (SPA) on the frontend communicating via REST and SignalR WebSockets to a C# ASP.NET Core 8 Backend using vertical slice architecture. The primary datastore is PostgreSQL with `pgvector` for embedding search, with Redis for caching and RabbitMQ for asynchronous background processing. 

A supplementary Python background service is responsible for complex video segmentation via the Gemini multimodal API.

## System Diagram

```mermaid
graph TD
    %% Users
    User[User / Web Browser]

    %% Frontend Layer
    subgraph Frontend [React SPA (Vite)]
        UI[React Components]
        State[React Query / Context]
        Router[React Router]
    end

    %% API Gateway / Routing
    subgraph Gateway [API Layer]
        Kestrel[Kestrel Web Server]
        SignalR[SignalR Hubs]
    end

    %% Backend Monolith (Vertical Slices)
    subgraph Backend [ASP.NET Core Web API]
        AuthSlice[Auth Module]
        ContentSlice[Content Module]
        SearchSlice[Search Module]
        QuickBoostSlice[QuickBoost AI Module]
        InteractionSlice[User Interaction Module]
    end

    %% Data & Infrastructure
    subgraph Infrastructure [Data & Messaging]
        PG[(PostgreSQL + pgvector)]
        Redis[(Redis Cache)]
        RMQ>RabbitMQ Message Broker]
    end

    %% External & AI Services
    subgraph External [External Services]
        Gemini[Google Gemini API]
        PythonAI[Python Video Analyzer]
        OAuth[Google OAuth2]
    end

    %% Connections
    User -->|HTTPS| Frontend
    Frontend -->|REST / WebSocket| Gateway
    Gateway --> Backend
    Backend -->|Read/Write| PG
    Backend -->|Cache| Redis
    Backend -->|Publish/Consume| RMQ
    
    %% AI connections
    Backend -->|LLM Prompts / Embeddings| Gemini
    Backend -->|HTTP POST| PythonAI
    AuthSlice -->|Verify Token| OAuth
    
    %% Background Workers
    RMQ -->|Consume AI Tasks| AIExtractionWorker[AI Extraction Background Service]
    AIExtractionWorker -->|Generate Summaries / Actions / Tags| Gemini
    AIExtractionWorker -->|Save Results| PG
```

## Features

### Ingest Content (Save URL)
*   **Purpose**: Allows users to save a URL to their digital brain. The system fetches the content, extracts text, categorizes it, and runs AI to summarize and extract action items.
*   **Frontend Components**: `Dashboard.jsx`, `SaveContentModal.jsx`, `api.js` (`saveUrl`)
*   **Backend Services**: `ContentController`, `SaveContentUseCase`, `AIExtractionWorker`, `ContentProcessorFactory`, `YouTubeImportService`
*   **API Endpoints**: `POST /api/v1/content`
*   **Database Tables**: `ContentItems`, `ContentPayloads`, `Tags`, `ContentItemTags`, `ActionItems`, `VideoSegments`
*   **LLM Usage**: Generates summaries, identifies relevant tags, extracts action steps, and calculates embeddings for semantic search.
*   **External Dependencies**: Google Gemini API, RabbitMQ

### Content Feed & Declutter
*   **Purpose**: Displays the ingested content to the user, allowing them to filter by energy level and platform. Users can archive, pin, or delete content.
*   **Frontend Components**: `Dashboard.jsx`, `ContentCard.jsx`, `api.js` (`getFeed`, `declutterItem`)
*   **Backend Services**: `ContentController`, `GetContentFeedUseCase`, `DeclutterContentUseCase`
*   **API Endpoints**: `GET /api/v1/content/feed`, `POST /api/v1/content/{id}/declutter`
*   **Database Tables**: `ContentItems`
*   **LLM Usage**: None directly on read.
*   **External Dependencies**: Redis (caching).

### QuickBoost Watch Plans (AI Reels)
*   **Purpose**: Automatically aggregates short, relevant video segments from the user's saved library matching a specific topic into a vertical, scrollable "Reel" format.
*   **Frontend Components**: `ProfileV2.jsx`, `QuickBoostReel.jsx`
*   **Backend Services**: `QuickBoostAIController`, `SemanticSearchUseCase`, `QuickBoostAIController` internal methods (`GetWatchPlan`)
*   **API Endpoints**: `GET /api/v1/quickboost/watch-plan`
*   **Database Tables**: `ContentPayloads`, `ContentItems`, `VideoSegments`
*   **LLM Usage**: Embeddings matching via `pgvector` (Cosine distance).
*   **External Dependencies**: YouTube Embed Iframe API.

### Semantic Search (RAG)
*   **Purpose**: Answers user questions using the user's saved knowledge base as context.
*   **Frontend Components**: `Dashboard.jsx` (Search Bar), `SearchModal.jsx`
*   **Backend Services**: `SearchController`, `RAGUseCase`, `SemanticSearchUseCase`, `AIExtractionService`
*   **API Endpoints**: `POST /api/v1/search/rag`
*   **Database Tables**: `ContentPayloads`, `ContentItems`
*   **LLM Usage**: Generates query embedding, performs vector search, and generates a conversational answer using the retrieved payloads as context.

### Authentication & Registration
*   **Purpose**: Securely authenticates users via local email/password or Google OAuth, issuing JWTs.
*   **Frontend Components**: `Login.jsx`, `Register.jsx`, `AuthProvider`
*   **Backend Services**: `AuthController`, `AuthUseCases`
*   **API Endpoints**: `POST /api/v1/auth/login`, `POST /api/v1/auth/register`, `POST /api/v1/auth/oauth`
*   **Database Tables**: `Users`, `UserIdentities`
*   **LLM Usage**: None.
*   **External Dependencies**: Google OAuth2 API.
