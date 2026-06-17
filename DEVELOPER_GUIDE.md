# Cortex — Developer Setup & Run Guide (No Docker)

This guide provides step-by-step instructions for setting up, configuring, and running the **Cortex Modular Monolith** application on your local machine **without using Docker**.

---

## Prerequisites

Before starting, ensure you have the following installed on your machine:

1. **.NET SDK 10.0** — [Download .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **EF Core CLI Tools** — Required for database migration commands. Install it globally via terminal:
   ```bash
   dotnet tool install --global dotnet-ef
   ```
   *(If already installed, you can update it using: `dotnet tool update --global dotnet-ef`)*

---

## Step 1: Install Services Natively (No Docker)

To run the application locally without Docker, you can install PostgreSQL (with the `pgvector` extension), Redis, and RabbitMQ directly on your host operating system.

### Option A: macOS Setup (via Homebrew)

If you are on macOS, you can easily manage native services using [Homebrew](https://brew.sh/):

```bash
# 1. Install and Start PostgreSQL
brew install postgresql
brew services start postgresql

# 2. Install and Start pgvector extension
# (Downloads & compiles the vector extension for your native Postgres install)
brew install pgvector

# 3. Install and Start Redis
brew install redis
brew services start redis

# 4. Install and Start RabbitMQ
brew install rabbitmq
# Add RabbitMQ to path (if Homebrew warns about it, or start via brew services)
brew services start rabbitmq
```

### Option B: Windows Setup

1. **PostgreSQL**: Download and run the [Interactive PostgreSQL Installer](https://www.postgresql.org/download/windows/).
2. **pgvector**: Download the pgvector binaries from the [pgvector GitHub releases](https://github.com/pgvector/pgvector/releases), select the version matching your PostgreSQL installation, and copy the `.dll`, `.sql`, and `.control` files into your Postgres directory folders (`lib` and `share/extension`).
3. **Redis**: Download the latest native Windows port from [Memurai](https://www.memurai.com/) or run it natively via WSL.
4. **RabbitMQ**: Download and install [Erlang/OTP](https://www.erlang.org/downloads) followed by the [RabbitMQ Windows Installer](https://www.rabbitmq.com/install-windows.html).

---

## Step 2: Set Up the Local Database

Once PostgreSQL is running natively, create the target database and user for Cortex. Connect to your local PostgreSQL server using `psql` or an administration tool like pgAdmin / DBeaver:

```sql
-- Connect as postgres superuser and run:
CREATE USER cortex WITH PASSWORD 'cortex_password';
CREATE DATABASE cortex_db OWNER cortex;
GRANT ALL PRIVILEGES ON DATABASE cortex_db TO cortex;

-- Connect to the newly created database and verify you can enable vector extension:
\c cortex_db;
CREATE EXTENSION IF NOT EXISTS vector;
```

---

## Step 3: Configure the Application

Edit the main configuration file at [appsettings.json](file:///Users/apple/Desktop/refind/refind/src/Cortex/appsettings.json) to point to your local native services:

```json
{
  "ConnectionStrings": {
    "CortexDatabase": "Host=localhost;Port=5432;Database=cortex_db;Username=cortex;Password=cortex_password"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "Cortex.API",
    "Audience": "Cortex.Clients",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "AIService": {
    "OpenAIApiKey": "",
    "EmbeddingModel": "text-embedding-3-small",
    "CompletionModel": "gpt-4o-mini"
  }
}
```

> [!TIP]
> **RabbitMQ Default Credentials**: For local native installations of RabbitMQ, the default username and password is `guest` / `guest` (unlike the docker credentials which use `cortex` / `cortex_dev`).

---

## Step 4: Apply Database Migrations

Cortex uses Entity Framework Core (EF Core) migrations to manage database schemas. The migration files are stored inside [Database/Migrations](file:///Users/apple/Desktop/refind/refind/src/Cortex/Database/Migrations/).

### How Migrations Work in the Monolith
EF Core maps your C# Entity classes in the `Modules` folder to database tables using configuration classes. When you apply migrations, the CLI tools inspect the `CortexDbContext` and compile the migration classes into SQL queries that run against your database.

### EF Core Migration Commands
From the repository root directory, run these commands:

1. **Apply Migrations** (creates tables and vector indexes in your local database):
   ```bash
   dotnet ef database update --project src/Cortex/Cortex.csproj
   ```

2. **Add a New Migration** (if you modify C# entity models in `Modules` during development):
   ```bash
   dotnet ef migrations add AddNewProperties --project src/Cortex/Cortex.csproj
   ```

3. **Remove Last Migration** (if you made a mistake and want to roll back the local migration files before applying them):
   ```bash
   dotnet ef migrations remove --project src/Cortex/Cortex.csproj
   ```

---

## Step 5: Run the Application

Start the web host and background AI workers:

```bash
dotnet run --project src/Cortex/Cortex.csproj
```

The server will start listening at:
- **HTTP Endpoint**: `http://localhost:5183`
- **Swagger Documentation**: `http://localhost:5183/swagger` (root page `http://localhost:5183/` redirects here automatically)
- **Health Check Endpoint**: `http://localhost:5183/api/v1/health`

---

## Step 6: Dev Fallbacks & True "Zero-Dependency" Mode

If you do not want to install all 3 native background services (PostgreSQL, Redis, RabbitMQ) during a quick feature change, the application handles failures gracefully:

### 1. Running Without RabbitMQ
The `AIExtractionWorker` hosted background service will attempt to connect to RabbitMQ on port `5672`. If it fails:
- It logs a connection error and retries every 10 seconds.
- **The rest of the Web API continues to run perfectly**.
- Synchronous endpoints (Auth, Drip, Feed retrieval) will work. Saving content will succeed, but the background AI extraction pipeline won't trigger because the broker is unreachable.

### 2. Running Without Redis
The cache multiplexer connects lazily. If Redis is down:
- The feed query Use Case (`GetContentFeedUseCase`) will catch the cache-miss/failure and read directly from PostgreSQL.
- The app remains fully operational.

### 3. Local Offline AI Fallback (No OpenAI Key needed)
If you leave `AIService:OpenAIApiKey` empty:
- **Summarization Fallback**: Uses local sentence parsing (takes the first 3 readable sentences).
- **Embedding Fallback**: Computes a deterministic local SHA-256 token hashing index normalized to a 1536-dimensional float vector.
- **Action Extraction Fallback**: Captures up to 5 readable task-oriented instructions from the text.
- This allows the entire ingestion and vector similarity search pipeline to run completely offline without cost or OpenAI connections.
