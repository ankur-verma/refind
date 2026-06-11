# Cortex — Launch Readiness Checklist

> Full checklist to go from current skeleton state to a working API that receives a real URL, parses it with LLM, stores/retrieves data from the database, and exposes Swagger documentation.

---

## Current Status Summary

| Component | Status | Details |
|---|---|---|
| **Cortex.API** | 🟡 Partial | Runs on `http://localhost:5183`, Swagger loads, health endpoint works. All other endpoints fail (missing DI). |
| **PostgreSQL** | ✅ Running | Docker container `cortex_postgres` on port 5432. pgvector extension installed. **No tables exist yet** (no migrations). |
| **Redis** | ✅ Running | Docker container `cortex_redis` on port 6379. Responds to PING. |
| **RabbitMQ** | ✅ Running | Docker container `cortex_rabbitmq` on port 5672. Management UI at `http://localhost:15672` (login: `cortex`/`cortex_dev`). |
| **Swagger UI** | ✅ Working | Available at `http://localhost:5183/swagger`. All endpoints documented but non-functional. |
| **EF Core Migrations** | ❌ Missing | No migrations exist. Database has no tables. |
| **DI Registration** | ❌ Disabled | Infrastructure, Persistence, and AI worker commented out in `Program.cs`. Use Cases not registered. |
| **IAuthService** | ❌ Not Implemented | Interface exists but no implementation class. |
| **IAIExtractionService** | ❌ Not Implemented | Interface exists but no implementation class. |
| **Content Processors** | 🟡 Stub | All 5 processors exist but return hardcoded placeholder data (no real URL parsing). |
| **appsettings.json** | ❌ Incomplete | Missing connection strings, Redis, RabbitMQ, JWT, and AI config sections. |

---

## Phase 1: Infrastructure & Configuration ✅🟡

> Goal: All external services connected and app starts without errors.

### Docker Containers

- [x] Docker Desktop installed
- [x] `docker compose up -d` — all 3 containers running
- [x] PostgreSQL (cortex_postgres) responds on port 5432
- [x] Redis (cortex_redis) responds to PING on port 6379
- [x] RabbitMQ (cortex_rabbitmq) responds on port 5672
- [x] RabbitMQ Management UI accessible at http://localhost:15672
- [x] pgvector extension installed in PostgreSQL (`vector` v0.5.1)

### Application Configuration

- [ ] **Update `appsettings.json`** with all connection strings and service configs:
  - [ ] `ConnectionStrings:CortexDatabase` — PostgreSQL connection string
  - [ ] `Redis:ConnectionString` — Redis connection (`localhost:6379`)
  - [ ] `RabbitMq` — host, port, user, password
  - [ ] `Jwt` — secret key, issuer, audience, expiration
  - [ ] `AIService` — OpenAI API key, model names
  - [ ] `GoogleOAuth` — client ID/secret (can be empty for now)
  - [ ] `Minio` — object storage config (can be empty for now)

### DI Registration

- [ ] **Uncomment** `builder.Services.AddInfrastructure(builder.Configuration)` in `Program.cs`
- [ ] **Uncomment** `builder.Services.AddPersistence(builder.Configuration)` in `Program.cs`
- [ ] **Uncomment** `builder.Services.AddAIWorker()` in `Program.cs`
- [ ] **Register Use Cases** in DI — currently NO use cases are registered. Need to add:
  ```csharp
  // Auth Use Cases
  builder.Services.AddScoped<RegisterUserUseCase>();
  builder.Services.AddScoped<LoginUserUseCase>();
  builder.Services.AddScoped<OAuthLoginUseCase>();
  builder.Services.AddScoped<RefreshTokenUseCase>();

  // Content Use Cases
  builder.Services.AddScoped<SaveContentUseCase>();
  builder.Services.AddScoped<GetContentFeedUseCase>();
  builder.Services.AddScoped<GetContentDetailUseCase>();
  builder.Services.AddScoped<DeleteContentUseCase>();
  builder.Services.AddScoped<DeclutterContentUseCase>();

  // Drip Use Cases
  builder.Services.AddScoped<CreateDripTrackUseCase>();
  builder.Services.AddScoped<AdvanceDripStepUseCase>();
  builder.Services.AddScoped<GetDripTracksUseCase>();

  // Search Use Cases
  builder.Services.AddScoped<SemanticSearchUseCase>();
  ```
- [ ] **Register `IAuthService`** implementation (needs to be created — see Phase 2)
- [ ] **Register `IAIExtractionService`** implementation (needs to be created — see Phase 3)

### Verify Startup

- [ ] App starts without DI errors after uncommenting layers
- [ ] App connects to PostgreSQL on startup
- [ ] App connects to Redis on startup
- [ ] App connects to RabbitMQ on startup
- [ ] Swagger UI loads with all endpoints visible
- [ ] No "Failed to determine https port" warning (already fixed)

---

## Phase 2: Database & Auth

> Goal: Database tables created, auth endpoints working (register/login → JWT).

### EF Core Migrations

- [ ] Install EF Core CLI tools: `dotnet tool install --global dotnet-ef`
- [ ] Create initial migration:
  ```bash
  cd src/Cortex.API
  dotnet ef migrations add InitialCreate --project ../Cortex.Persistence
  ```
- [ ] Apply migration to database:
  ```bash
  dotnet ef database update --project ../Cortex.Persistence
  ```
- [ ] Verify tables created in PostgreSQL:
  ```bash
  docker exec cortex_postgres psql -U cortex -d cortex_db -c "\dt"
  ```
  Expected tables: `Users`, `UserIdentities`, `Subscriptions`, `ContentItems`, `ContentPayloads`, `ActionItems`, `DripTracks`, `DripSteps`, `Tags`, `ContentItemTags`

### Implement `IAuthService`

The `IAuthService` interface is defined but **no implementation exists**. Need to create `AuthService` in `Cortex.Infrastructure` with:

- [ ] `RegisterAsync` — create user, hash password (BCrypt), create default subscription, generate JWT
- [ ] `LoginAsync` — validate credentials, generate JWT
- [ ] `OAuthLoginAsync` — Google OAuth token validation, user creation/lookup
- [ ] `RefreshTokenAsync` — validate refresh token, generate new JWT pair

Key dependencies: `IUserRepository`, `ISubscriptionRepository`, `IUnitOfWork`, `JwtSettings`, `BCrypt.Net`

### Register & DI Wire

- [ ] Register `IAuthService` → `AuthService` in `Infrastructure/DependencyInjection.cs`
- [ ] JWT Authentication middleware configured in `Program.cs`:
  ```csharp
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options => { /* configure token validation */ });
  ```

### Verify Auth Flow

- [ ] `POST /api/v1/auth/register` — creates user, returns JWT
- [ ] `POST /api/v1/auth/login` — validates credentials, returns JWT
- [ ] Protected endpoints return 401 without JWT
- [ ] Protected endpoints return 200 with valid JWT

---

## Phase 3: Content Ingestion Pipeline (URL → LLM → DB)

> Goal: User sends a URL → app parses it → LLM processes it → data stored in DB → retrievable via API.

### Implement `IAIExtractionService`

This is the LLM integration. Interface has 3 methods — all need implementation:

- [ ] `GenerateSummaryAsync(rawText)` — send text to OpenAI GPT-4o-mini, get summary back
- [ ] `GenerateEmbeddingAsync(text)` — send text to OpenAI text-embedding-3-small, get float[] back
- [ ] `ExtractActionsAsync(rawText)` — send text to LLM, parse structured action items

Implementation location: `Cortex.Infrastructure/` or `Cortex.AI/`

Dependencies needed:
- [ ] OpenAI NuGet package (e.g., `OpenAI` or `Azure.AI.OpenAI`)
- [ ] Valid OpenAI API key in `appsettings.json`

### Implement Real Content Processors

All 5 content processors currently return **hardcoded stub data**. At minimum, the `WebPageContentProcessor` needs to extract real content:

- [ ] **WebPageContentProcessor** — use `HttpClient` + `HtmlAgilityPack` to scrape page title and body text
- [ ] **YouTubeContentProcessor** — YouTube Data API v3 for metadata + captions (or use yt-dlp for transcripts)
- [ ] InstagramContentProcessor — stub OK for MVP
- [ ] TikTokContentProcessor — stub OK for MVP
- [ ] PdfContentProcessor — stub OK for MVP

NuGet packages needed:
- [ ] `HtmlAgilityPack` — HTML parsing and text extraction

### Verify End-to-End Flow

1. **Save content:**
   - [ ] `POST /api/v1/content` with `{"url": "https://example.com/article"}` → returns 201 + ContentItem ID
   - [ ] Verify ContentItem saved in PostgreSQL with status `Processing`
   - [ ] Verify message published to RabbitMQ `ai_extraction_queue`

2. **AI Worker processes:**
   - [ ] AI Worker picks up message from queue
   - [ ] Content processor extracts text from URL
   - [ ] LLM generates summary
   - [ ] LLM generates embedding vector
   - [ ] LLM extracts action items
   - [ ] ContentPayload saved in DB (summary, raw text, embedding)
   - [ ] ActionItems saved in DB
   - [ ] Tags created/linked
   - [ ] ContentItem status updated to `Ready`

3. **Retrieve content:**
   - [ ] `GET /api/v1/content/feed` — returns processed content with status `Ready`
   - [ ] `GET /api/v1/content/{id}` — returns full detail with summary, action items, tags
   - [ ] Verify Redis caching works (second feed request served from cache)

4. **Search content:**
   - [ ] `POST /api/v1/search` with `{"query": "machine learning"}` → returns semantically similar results
   - [ ] Verify pgvector cosine similarity query works

---

## Phase 4: Remaining Features

> Goal: All API endpoints fully functional.

### Content Management

- [ ] `DELETE /api/v1/content/{id}` — deletes content and cascading records
- [ ] `POST /api/v1/content/{id}/declutter` — Pin action works
- [ ] `POST /api/v1/content/{id}/declutter` — Archive action works
- [ ] `POST /api/v1/content/{id}/declutter` — Delete action works

### Drip System

- [ ] `POST /api/v1/drip` — creates a drip track (requires AI to slice content into daily micro-steps)
- [ ] `GET /api/v1/drip/tracks` — lists user's drip tracks
- [ ] `PATCH /api/v1/drip/steps/{id}` — marks step as complete, advances track

### Caching

- [ ] Content feed responses cached in Redis for 5 minutes
- [ ] Cache invalidated when content is created/updated/deleted
- [ ] Verify with `docker exec cortex_redis redis-cli KEYS '*'`

---

## Phase 5: Polish & Production Readiness

### Error Handling

- [ ] Global exception middleware handles all edge cases
- [ ] Proper error messages returned for invalid URLs
- [ ] Proper error messages for duplicate URL saves
- [ ] Rate limiting on content save endpoint

### Logging

- [ ] Serilog configured with structured logging
- [ ] Log AI processing pipeline steps
- [ ] Log RabbitMQ message publish/consume events

### Testing

- [ ] Unit tests for Use Cases (tests/ directory is currently empty)
- [ ] Integration tests for repository queries
- [ ] API endpoint tests

### API Documentation

- [ ] Swagger UI shows all endpoints with descriptions ✅ (already done)
- [ ] Request/response examples in Swagger
- [ ] Proper HTTP status codes documented

---

## Quick Reference: Service Connectivity

### Verified ✅ (as of last check)

```
┌────────────────────────────────────────────────────────────┐
│  Cortex.API (http://localhost:5183)                         │
│  ├── Swagger UI: http://localhost:5183/swagger     ✅       │
│  └── Health:     http://localhost:5183/api/v1/health ✅     │
├────────────────────────────────────────────────────────────┤
│  PostgreSQL (localhost:5432)                                │
│  ├── Container: cortex_postgres                    ✅       │
│  ├── Version: PostgreSQL 15.4                      ✅       │
│  ├── pgvector: v0.5.1                              ✅       │
│  └── Tables: NONE (migrations not run)             ❌       │
├────────────────────────────────────────────────────────────┤
│  Redis (localhost:6379)                                     │
│  ├── Container: cortex_redis                       ✅       │
│  ├── PING → PONG                                   ✅       │
│  └── App connected: No (DI disabled)               ❌       │
├────────────────────────────────────────────────────────────┤
│  RabbitMQ (localhost:5672)                                  │
│  ├── Container: cortex_rabbitmq                    ✅       │
│  ├── Version: RabbitMQ 3.13.7                      ✅       │
│  ├── Management UI: http://localhost:15672          ✅       │
│  ├── Login: cortex / cortex_dev                    ✅       │
│  └── App connected: No (DI disabled)               ❌       │
└────────────────────────────────────────────────────────────┘
```

### What's Blocking the Full Flow

```
User sends URL
    │
    ▼
API receives request ──── ✅ Controller exists
    │
    ▼
Save to PostgreSQL ─────── ❌ No DI, no migrations, no tables
    │
    ▼
Publish to RabbitMQ ────── ❌ No DI, not connected
    │
    ▼
AI Worker consumes ──────── ❌ Not registered, no IAIExtractionService impl
    │
    ▼
Content processor ────────── 🟡 Stubs exist, no real URL parsing
    │
    ▼
LLM processes text ──────── ❌ No IAIExtractionService implementation
    │
    ▼
Save payload + embedding ── ❌ No DB tables
    │
    ▼
Retrieve via GET ────────── ❌ No data to retrieve
    │
    ▼
Cache in Redis ──────────── ❌ Not connected
```

### Priority Order to Unblock

| Priority | Task | Blocks |
|---|---|---|
| **P0** | Update `appsettings.json` | Everything |
| **P0** | Uncomment DI lines + register Use Cases | Everything |
| **P0** | Implement `IAuthService` | All protected endpoints |
| **P0** | Create EF migrations + apply | All DB operations |
| **P1** | Implement `IAIExtractionService` | AI processing, search, drip |
| **P1** | Implement real `WebPageContentProcessor` | Real URL parsing |
| **P2** | Implement remaining content processors | YouTube, Instagram, TikTok, PDF |
| **P3** | Add cache invalidation | Stale data in Redis |
| **P3** | Add tests | Quality assurance |
