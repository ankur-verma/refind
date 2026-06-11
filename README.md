# Cortex — AI-Powered Content Intelligence Backend

Cortex is the backend API for [refind.ai](https://refind.ai), an AI-powered content curation and learning platform. It ingests URLs (YouTube, Instagram, TikTok, Web, PDF), processes them through an AI pipeline to extract summaries, embeddings, and action items, and serves a mood-filtered "Cognitive Dashboard" feed to clients.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Full Setup Guide (macOS)](#full-setup-guide-macos)
  - [Step 1: Install Docker Desktop](#step-1-install-docker-desktop)
  - [Step 2: Start Infrastructure Containers](#step-2-start-infrastructure-containers)
  - [Step 3: Configure the Application](#step-3-configure-the-application)
  - [Step 4: Run the Application](#step-4-run-the-application)
  - [Step 5: Apply Database Migrations](#step-5-apply-database-migrations)
- [Troubleshooting](#troubleshooting)
- [Project Structure](#project-structure)
- [Architecture & Layer Interactions](#architecture--layer-interactions)
- [API Endpoints](#api-endpoints)
- [External Services & Data Stores](#external-services--data-stores)
- [Redis Analysis](#redis-analysis)
- [PostgreSQL Database Analysis](#postgresql-database-analysis)
- [RabbitMQ Analysis](#rabbitmq-analysis)
- [Known Issues & Current State](#known-issues--current-state)

---

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| .NET SDK | **10.0** | Runtime & build |
| Docker Desktop | Latest | Runs PostgreSQL, Redis, RabbitMQ in containers |
| (Optional) Rider / VS Code | — | IDE |

---

## Full Setup Guide (macOS)

### Step 1: Install Docker Desktop

Docker is **required** to run PostgreSQL, Redis, and RabbitMQ. All three run as Docker containers defined in `docker-compose.yml`.

#### Option A: Install via Homebrew (Recommended)

```bash
# Install Homebrew if not already installed
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install Docker Desktop
brew install --cask docker
```

After installation:
1. **Open Docker Desktop** from Spotlight (⌘+Space → type "Docker" → Enter)
2. **Wait for Docker to finish starting** — look for the Docker whale icon in the menu bar showing "Docker Desktop is running"
3. **Verify installation:**
   ```bash
   docker --version
   # Expected: Docker version 2x.x.x, build xxxxx
   
   docker compose version
   # Expected: Docker Compose version v2.x.x
   ```

#### Option B: Install via DMG

1. Go to [https://www.docker.com/products/docker-desktop/](https://www.docker.com/products/docker-desktop/)
2. Download the DMG for **Apple Silicon** (M1/M2/M3/M4) or **Intel** based on your Mac
3. Open the DMG, drag Docker to Applications
4. Launch Docker Desktop, grant permissions when prompted
5. Wait for it to fully start (whale icon stable in menu bar)

> **⚠️ Docker Desktop must be running** before you can execute any `docker` or `docker compose` commands. If you get "Cannot connect to the Docker daemon", open Docker Desktop first.

---

### Step 2: Start Infrastructure Containers

From the repository root:

```bash
cd /path/to/Cortex
docker compose up -d
```

This starts three containers:

| Service | Container Name | Port(s) | Dashboard |
|---|---|---|---|
| PostgreSQL + pgvector | `cortex_postgres` | `5432` | — |
| Redis | `cortex_redis` | `6379` | — |
| RabbitMQ | `cortex_rabbitmq` | `5672`, `15672` | [http://localhost:15672](http://localhost:15672) |

**Default credentials** (from `docker-compose.yml`):
- **PostgreSQL**: user=`cortex`, password=`cortex_password`, db=`cortex_db`
- **RabbitMQ**: user=`cortex`, password=`cortex_dev`
- **Redis**: no password (default `localhost:6379`)

#### Verify containers are running:

```bash
docker compose ps
```

Expected output:
```
NAME              STATUS    PORTS
cortex_postgres   Up        0.0.0.0:5432->5432/tcp
cortex_redis      Up        0.0.0.0:6379->6379/tcp
cortex_rabbitmq   Up        0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

#### Test individual services:

```bash
# Test Redis
docker exec cortex_redis redis-cli ping
# Expected: PONG

# Test PostgreSQL
docker exec cortex_postgres psql -U cortex -d cortex_db -c "SELECT 1;"
# Expected: 1

# Test RabbitMQ (open in browser)
open http://localhost:15672
# Login: cortex / cortex_dev
```

#### Useful Docker commands:

```bash
# Stop all containers
docker compose down

# Stop and remove all data (fresh start)
docker compose down -v

# View container logs
docker compose logs -f redis
docker compose logs -f postgres
docker compose logs -f rabbitmq

# Restart a specific service
docker compose restart redis
```

---

### Step 3: Configure the Application

The current `appsettings.json` is minimal. You need to add connection strings and service settings.

Edit `src/Cortex.API/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "ConnectionStrings": {
    "CortexDatabase": "Host=localhost;Port=5432;Database=cortex_db;Username=cortex;Password=cortex_password"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "cortex",
    "Password": "cortex_dev"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "Cortex.API",
    "Audience": "Cortex.Clients",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "GoogleOAuth": {
    "ClientId": "",
    "ClientSecret": ""
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "",
    "SecretKey": "",
    "BucketName": "cortex-content"
  },
  "AIService": {
    "OpenAIApiKey": "",
    "EmbeddingModel": "text-embedding-3-small",
    "CompletionModel": "gpt-4o-mini"
  }
}
```

Then **uncomment** the DI registration lines in `src/Cortex.API/Program.cs` (lines 10–12):

```csharp
// Change from:
// builder.Services.AddInfrastructure(builder.Configuration);
// builder.Services.AddPersistence(builder.Configuration);
// builder.Services.AddAIWorker();

// To:
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddAIWorker();
```

> **⚠️ Important:** You must have Docker containers running (Step 2) BEFORE uncommenting these lines. Otherwise the app will crash on startup because it tries to connect to Redis and RabbitMQ during DI registration.

---

### Step 4: Run the Application

```bash
cd src/Cortex.API
dotnet run
```

The API starts on **http://localhost:5183**.

| URL | Description |
|---|---|
| [http://localhost:5183/](http://localhost:5183/) | Redirects to Swagger UI |
| [http://localhost:5183/swagger](http://localhost:5183/swagger) | Swagger API documentation |
| [http://localhost:5183/api/v1/health](http://localhost:5183/api/v1/health) | Health check endpoint |

---

### Step 5: Apply Database Migrations (after uncommenting Persistence)

Once you've enabled the Persistence layer (Step 3), create and apply migrations:

```bash
# Install EF Core CLI tools (one-time)
dotnet tool install --global dotnet-ef

# Create initial migration
cd src/Cortex.API
dotnet ef migrations add InitialCreate --project ../Cortex.Persistence

# Apply migration to database
dotnet ef database update --project ../Cortex.Persistence
```

---

## Troubleshooting

### 404 on Root URL

**Problem:** Hitting `http://localhost:5183/` returns 404.

**Solution:** Fixed — the root URL now redirects to `/swagger`. If you're still seeing 404, make sure you've rebuilt after the latest changes:
```bash
cd src/Cortex.API
dotnet run
```

### "Address already in use" (Port 5183)

**Problem:** `dotnet run` fails with "address already in use".

**Solution:** Kill the existing process:
```bash
lsof -ti:5183 | xargs kill -9
dotnet run
```

### "Failed to determine the https port for redirect"

**Problem:** Warning in console about HTTPS port.

**Solution:** Fixed — HTTPS redirection has been removed since the dev profile uses HTTP only.

### "Cannot connect to the Docker daemon"

**Problem:** Docker commands fail.

**Solution:** Open Docker Desktop app and wait for it to fully start (whale icon in menu bar).

### Redis / RabbitMQ Connection Refused on Startup

**Problem:** App crashes immediately after uncommenting `AddInfrastructure()`.

**Solution:** Ensure Docker containers are running:
```bash
docker compose up -d
docker compose ps   # verify all 3 are "Up"
```

### EF Core Version Conflict Warnings (MSB3277)

**Problem:** Build warns about conflicting `Microsoft.EntityFrameworkCore` versions (10.0.4 vs 10.0.9).

**Cause:** The `Pgvector.EntityFrameworkCore` (v0.3.0) package pulls in EF Core 10.0.4, while `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.2) references EF Core 10.0.9.

**Solution:** These are warnings, not errors. The app runs fine. To suppress, you can pin the EF Core version by adding to `Cortex.Persistence.csproj`:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
```

---

## Project Structure

```
Cortex/
├── Cortex.slnx                          # Solution file
├── docker-compose.yml                   # PostgreSQL + Redis + RabbitMQ
├── README.md                            # This file
├── src/
│   ├── Cortex.API/                      # ASP.NET Core Web API (entry point)
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs        # /api/v1/auth — register, login, OAuth, refresh
│   │   │   ├── ContentController.cs     # /api/v1/content — save, feed, detail, delete, declutter
│   │   │   ├── DripController.cs        # /api/v1/drip — create track, get tracks, advance step
│   │   │   ├── SearchController.cs      # /api/v1/search — semantic vector search
│   │   │   └── HealthController.cs      # /api/v1/health — health check
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs  # Global exception → JSON error response
│   │   ├── Properties/
│   │   │   └── launchSettings.json      # Dev server config (port 5183)
│   │   ├── Program.cs                   # App entry point & DI wiring
│   │   └── appsettings.json             # Configuration
│   │
│   ├── Cortex.Application/             # Application layer (Use Cases, DTOs, Interfaces)
│   │   ├── UseCases/
│   │   │   ├── Auth/AuthUseCases.cs     # Register, Login, OAuth, RefreshToken
│   │   │   ├── Content/ContentUseCases.cs  # Save, Feed, Detail, Delete, Declutter
│   │   │   ├── Drip/DripUseCases.cs     # CreateTrack, AdvanceStep, GetTracks
│   │   │   └── Search/SemanticSearchUseCase.cs  # NL query → embedding → vector search
│   │   ├── Interfaces/
│   │   │   ├── Services/IServices.cs    # IAuthService, ICacheService, IMessageBroker, etc.
│   │   │   ├── Repositories/           # IUserRepository, IContentItemRepository, etc.
│   │   │   └── Factories/              # IContentProcessorFactory
│   │   ├── DTOs/                        # Request/Response DTOs
│   │   └── Validators/                  # Input validation
│   │
│   ├── Cortex.Domain/                   # Domain layer (Entities, Events)
│   │   ├── Entities/
│   │   │   ├── User.cs, UserIdentity.cs
│   │   │   ├── ContentItem.cs, ContentPayload.cs
│   │   │   ├── ActionItem.cs, Tag.cs, ContentItemTag.cs
│   │   │   ├── DripTrack.cs, DripStep.cs
│   │   │   └── Subscription.cs
│   │   ├── Events/                      # Domain events
│   │   └── Interfaces/IDomainEvent.cs
│   │
│   ├── Cortex.Infrastructure/           # Infrastructure layer (External integrations)
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs     # Redis-backed ICacheService
│   │   ├── Messaging/
│   │   │   └── RabbitMqMessageBroker.cs # RabbitMQ IMessageBroker
│   │   ├── ContentProcessing/
│   │   │   ├── ContentProcessors.cs     # YouTube, Web, Instagram, PDF, TikTok processors
│   │   │   └── Factories/              # ContentProcessorFactory
│   │   ├── Settings/Settings.cs         # RedisSettings, RabbitMqSettings, JwtSettings, etc.
│   │   └── DependencyInjection.cs       # AddInfrastructure() — Redis, RabbitMQ, etc.
│   │
│   ├── Cortex.Persistence/             # Persistence layer (EF Core + PostgreSQL)
│   │   ├── CortexDbContext.cs           # EF Core DbContext with pgvector
│   │   ├── Configurations/
│   │   │   └── EntityConfigurations.cs  # Fluent API entity configs
│   │   ├── Repositories/
│   │   │   ├── Repositories.cs          # User, ContentItem, ContentPayload repos
│   │   │   └── RemainingRepositories.cs # ActionItem, DripTrack, Tag, Subscription, UnitOfWork
│   │   └── DependencyInjection.cs       # AddPersistence() — DbContext & repos
│   │
│   ├── Cortex.AI/                       # AI Worker layer
│   │   ├── Workers/AIExtractionWorker.cs  # BackgroundService consuming RabbitMQ
│   │   └── DependencyInjection.cs       # AddAIWorker()
│   │
│   └── Cortex.SharedKernel/            # Shared kernel
│       ├── BaseEntity.cs, AuditableEntity.cs
│       ├── Result.cs                    # Result<T> monad
│       ├── Guard.cs                     # Input validation guards
│       ├── PagedList.cs                 # Paged result wrapper
│       ├── Enums/                       # PlatformType, ContentStatus, EnergyLevel, etc.
│       └── Exceptions/                  # NotFoundException, ValidationException, etc.
│
└── tests/                               # (Empty — no tests yet)
```

---

## Architecture & Layer Interactions

The project follows **Clean Architecture** (Onion Architecture):

```
┌──────────────────────────────────────────────────────────────────┐
│                         Cortex.API                               │
│   Controllers → Use Cases → Interfaces ← Implementations        │
│   References: Application, Infrastructure, Persistence, AI       │
└────────────┬─────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Cortex.Application                           │
│   Use Cases, Interfaces (ICacheService, IRepositories, etc.)     │
│   References: Domain, SharedKernel                               │
└────────────┬─────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────────┐
│                      Cortex.Domain                               │
│   Entities (User, ContentItem, DripTrack, etc.)                  │
│   References: SharedKernel                                       │
└────────────┬─────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Cortex.SharedKernel                            │
│   BaseEntity, Result<T>, Guard, PagedList, Enums, Exceptions     │
└──────────────────────────────────────────────────────────────────┘
```

**Outer ring implementations** (depend on Application interfaces):

```
┌───────────────────────┐    ┌───────────────────────┐    ┌───────────────────┐
│  Cortex.Infrastructure│    │  Cortex.Persistence   │    │    Cortex.AI      │
│  Implements:          │    │  Implements:          │    │  AIExtraction-    │
│  • ICacheService      │    │  • IUserRepository    │    │  Worker           │
│    → Redis            │    │  • IContentItemRepo   │    │  (BackgroundSvc   │
│  • IMessageBroker     │    │  • IDripTrackRepo     │    │   consuming       │
│    → RabbitMQ         │    │  • IUnitOfWork        │    │   RabbitMQ)       │
│  • ContentProcessors  │    │  • CortexDbContext    │    │                   │
└───────────────────────┘    └───────────────────────┘    └───────────────────┘
```

### Request Flow

```
Client HTTP Request
    │
    ▼
Cortex.API (Controller)
    │
    ▼
ExceptionHandlingMiddleware (catches unhandled exceptions)
    │
    ▼
Use Case (Cortex.Application)
    │
    ├──→ Repository (Cortex.Persistence) ──→ PostgreSQL
    ├──→ ICacheService (Cortex.Infrastructure) ──→ Redis
    └──→ IMessageBroker (Cortex.Infrastructure) ──→ RabbitMQ
                                                      │
                                                      ▼
                                            AIExtractionWorker (Cortex.AI)
                                                      │
                                                      ▼
                                            AI Pipeline: Extract → Summarize
                                            → Embed → Save to PostgreSQL
```

---

## API Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/` | No | Redirects to Swagger UI |
| `GET` | `/api/v1/health` | No | Health check |
| `POST` | `/api/v1/auth/register` | No | Register new user |
| `POST` | `/api/v1/auth/login` | No | Login with email/password |
| `POST` | `/api/v1/auth/oauth` | No | OAuth login (Google) |
| `POST` | `/api/v1/auth/refresh` | No | Refresh JWT token |
| `POST` | `/api/v1/content` | JWT | Save URL for AI ingestion |
| `GET` | `/api/v1/content/feed` | JWT | Get mood-filtered content feed |
| `GET` | `/api/v1/content/{id}` | JWT | Get content detail with AI payload |
| `DELETE` | `/api/v1/content/{id}` | JWT | Delete content item |
| `POST` | `/api/v1/content/{id}/declutter` | JWT | Pin/Archive/Delete (Swipe to Clean) |
| `POST` | `/api/v1/drip` | JWT | Create a Drip Track |
| `GET` | `/api/v1/drip/tracks` | JWT | List user's Drip Tracks |
| `PATCH` | `/api/v1/drip/steps/{id}` | JWT | Complete a Drip Step |
| `POST` | `/api/v1/search` | JWT | Semantic vector search |

---

## External Services & Data Stores

| Service | Technology | NuGet Package | Purpose |
|---|---|---|---|
| **Database** | PostgreSQL + pgvector | `Npgsql.EntityFrameworkCore.PostgreSQL`, `Pgvector.EntityFrameworkCore` | Primary data store + vector search |
| **Cache** | Redis 7 | `StackExchange.Redis` v2.13.17 | Response caching (content feed) |
| **Message Broker** | RabbitMQ 3 | `RabbitMQ.Client` v7.2.1 | Async AI processing queue |
| **Auth** | JWT Bearer | `Microsoft.AspNetCore.Authentication.JwtBearer` | Token-based auth |
| **Password** | BCrypt | `BCrypt.Net-Next` | Password hashing |
| **Logging** | Serilog | `Serilog.AspNetCore` | Structured logging |

---

## Redis Analysis

### Is Redis Actually Connected and Used?

**Answer: The code is properly wired to use Redis, but it is currently NOT active at runtime.**

#### ✅ What's Implemented

1. **NuGet Package**: `StackExchange.Redis` v2.13.17 in `Cortex.Infrastructure.csproj`

2. **Settings class**: `RedisSettings` with default `localhost:6379`

3. **DI Registration** (`Infrastructure/DependencyInjection.cs`):
   ```csharp
   services.AddSingleton<IConnectionMultiplexer>(
       ConnectionMultiplexer.Connect(redisSettings.ConnectionString));
   services.AddScoped<ICacheService, RedisCacheService>();
   ```

4. **Full `RedisCacheService`** with 4 real Redis operations:
   - `GetAsync<T>` → `StringGet` + JSON deserialization
   - `SetAsync<T>` → `StringSet` + JSON serialization (default 30 min TTL)
   - `RemoveAsync` → `KeyDelete`
   - `ExistsAsync` → `KeyExists`

5. **Real usage in business logic** — `GetContentFeedUseCase` (cache-aside pattern):
   ```csharp
   var cacheKey = $"feed:{userId}:{query.EnergyLevel}:{query.PlatformType}:{query.Page}";
   var cached = await _cache.GetAsync<PagedList<ContentItem>>(cacheKey, ct);
   if (cached is not null) return cached;
   // ... fetch from DB ...
   await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
   ```

#### ❌ Why It's NOT Active

`AddInfrastructure()` is **commented out** in `Program.cs`, so the Redis connection and cache service are never registered.

#### Verdict

| Aspect | Status |
|---|---|
| Docker container defined | ✅ |
| NuGet package referenced | ✅ |
| Full cache service implemented | ✅ |
| Used in business logic (content feed) | ✅ |
| **Registered at startup** | ❌ Commented out |
| **Actually receiving traffic** | ❌ No |

---

## PostgreSQL Database Analysis

### Is the Database Actually Connected and Used?

**Answer: Fully implemented, but NOT active at runtime.**

#### ✅ What's Implemented

- **10 DbSets** covering all entities
- **Fluent API configurations** with indexes, constraints, cascading deletes
- **pgvector HNSW index** for semantic similarity search
- **8 repository implementations** with real EF Core queries
- **Vector search**: `CosineDistance()` for semantic content search

#### ❌ Why It's NOT Active

`AddPersistence()` is **commented out** in `Program.cs`, and `appsettings.json` has no connection string.

---

## RabbitMQ Analysis

### Is RabbitMQ Actually Connected and Used?

**Answer: Implemented, but NOT active.**

- `RabbitMqMessageBroker` publishes messages with durable queues
- `AIExtractionWorker` consumes `ai_extraction_queue` (BackgroundService)
- `SaveContentUseCase` publishes to the queue when content is saved

Both publisher and consumer are disabled because `AddInfrastructure()` and `AddAIWorker()` are commented out.

---

## Known Issues & Current State

### ⚠️ DI Registration Disabled

The three critical lines in `Program.cs` are **commented out**:

```csharp
// builder.Services.AddInfrastructure(builder.Configuration);   // Redis, RabbitMQ
// builder.Services.AddPersistence(builder.Configuration);      // PostgreSQL
// builder.Services.AddAIWorker();                              // AI Worker
```

### What Works Right Now
- ✅ Application starts on `http://localhost:5183`
- ✅ Root URL `/` redirects to Swagger UI
- ✅ Swagger UI loads and documents all endpoints
- ✅ `/api/v1/health` returns healthy status
- ✅ Global exception handling middleware

### What Does NOT Work
- ❌ Auth, Content, Drip, Search endpoints (dependencies not registered)
- ❌ Redis, PostgreSQL, RabbitMQ connections
- ❌ AI background processing
- ❌ No EF Core migrations exist
- ❌ No `IAuthService` implementation exists

### To Fully Activate

1. Install Docker Desktop and start containers (see [Step 1](#step-1-install-docker-desktop) & [Step 2](#step-2-start-infrastructure-containers))
2. Update `appsettings.json` (see [Step 3](#step-3-configure-the-application))
3. Uncomment DI lines in `Program.cs`
4. Create and apply EF Core migrations (see [Step 5](#step-5-apply-database-migrations))
5. Implement `IAuthService` (not yet in the codebase)
