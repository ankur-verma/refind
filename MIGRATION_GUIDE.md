# Code Migration & Setup Guide

This document outlines how to set up and migrate the **Refind (Cortex)** codebase to a new machine. 

**The codebase is entirely self-contained.** Everything required to run the application (including backend logic, frontend UI, media extraction pipelines, and AI orchestrations) is checked into the GitHub repository.

You do **not** need to install complex external microservices. The architecture is designed so that the only external dependencies you need to provide are the database and any necessary master data.

---

## 1. Prerequisites

Before cloning the repository, ensure the target machine has the following installed:

### Required Infrastructure
- **PostgreSQL Database** (v15+)
- **pgvector Extension**: The database *must* have the `pgvector` extension installed, as the application relies heavily on `Pgvector.EntityFrameworkCore` for AI embeddings and semantic search.

### Required Development SDKs
- **.NET SDK 10.0+**: Required to compile and run the Cortex backend.
- **Node.js (v20+) & npm**: Required to run the Vite/React frontend.
- **yt-dlp & ffmpeg**: Required to be installed on the host machine and accessible in the system `PATH` for local media extraction.

---

## 2. Migration & Setup Sequence

If an AI Agent (or developer) is migrating this project to a new machine, follow this exact sequence:

### Step 1: Clone the Repository
Clone the codebase from GitHub to your local machine.

### Step 2: Configure Database & Master Data (macOS)
Since you are setting this up on a MacBook, the recommended and easiest way to install PostgreSQL with the vector extension is via Homebrew.

1. **Install PostgreSQL and pgvector via Homebrew**:
   ```bash
   # Install PostgreSQL (v16 recommended) and pgvector
   brew install postgresql@16
   brew install pgvector
   
   # Start the PostgreSQL service
   brew services start postgresql@16
   ```

2. **Create the Empty Database (or Restore Backup)**:
   The backend application is programmed to **automatically run EF Core migrations on startup**. It will automatically create all tables, schemas, and enable the `vector` extension for you. You only need to create an empty database, OR restore a full backup if you are migrating existing data.
   
   *To create an empty database:*
   ```bash
   psql postgres -c "CREATE DATABASE cortex_db;"
   ```
   
   *OR, to restore an existing backup:*
   ```bash
   psql cortex_db < master_data.sql
   ```

*(Note: You do not need to manually run `dotnet ef database update`. The application's `DatabaseSeeder` handles this automatically upon starting `dotnet run`.)*

### Step 3: Configure Environment Variables
Copy `src/Cortex/appsettings.json` to `src/Cortex/appsettings.Development.json` and configure the necessary secrets:
- `ConnectionStrings:CortexDatabase` (Point this to your PostgreSQL instance)
- `AIService:Gemini:OpenAIApiKey` (Required for LLM processing)
- `GoogleOAuth:ClientId` & `ClientSecret` (Required for Login & YouTube Import)

### Step 4: Start the Backend
```bash
cd src/Cortex
dotnet run
```

### Step 5: Start the Frontend
In a separate terminal:
```bash
cd frontend
npm install
npm run dev
```

---

## Architecture Note

While the codebase references **Redis**, **RabbitMQ**, and **MinIO** in the configuration files, the core application relies primarily on **PostgreSQL + pgvector** as the single source of truth for semantic search, user data, and content storage. As long as Postgres is configured and the codebase is cloned, the application logic is ready to execute.
