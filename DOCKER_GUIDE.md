# Docker Infrastructure Guide — Cortex

This document explains how Docker works in the Cortex project, how each container is created and configured, and how to interact with each service.

---

## Table of Contents

- [What is Docker?](#what-is-docker)
- [How Docker Compose Works](#how-docker-compose-works)
- [The docker-compose.yml Explained](#the-docker-composeyml-explained)
  - [PostgreSQL + pgvector](#1-postgresql--pgvector)
  - [Redis](#2-redis)
  - [RabbitMQ](#3-rabbitmq)
  - [Volumes](#4-volumes)
- [How Containers Were Created](#how-containers-were-created)
- [RabbitMQ Management UI — Login & Usage](#rabbitmq-management-ui--login--usage)
- [Common Docker Commands](#common-docker-commands)
- [How Cortex Connects to These Services](#how-cortex-connects-to-these-services)

---

## What is Docker?

Docker is a tool that lets you run applications inside **containers** — lightweight, isolated environments that bundle everything an application needs (OS libraries, binaries, config) into a single package.

Think of it like this:

| Concept | Analogy |
|---|---|
| **Docker Image** | A recipe / blueprint (e.g., "Redis 7 on Alpine Linux") |
| **Docker Container** | A running instance of that recipe (e.g., your actual Redis server) |
| **Docker Hub** | An online store of pre-built recipes (images) |
| **Docker Compose** | A tool to run multiple containers together from a single config file |

**Why use Docker for Cortex?**

Instead of manually installing PostgreSQL, Redis, and RabbitMQ on your Mac (dealing with version conflicts, config files, background services), Docker lets you spin them all up with a single command:

```bash
docker compose up -d
```

And tear them all down with:

```bash
docker compose down
```

---

## How Docker Compose Works

Docker Compose reads a file called `docker-compose.yml` and:

1. **Pulls images** from Docker Hub (if not already downloaded)
2. **Creates named volumes** for persistent data storage
3. **Creates a private network** so containers can talk to each other
4. **Starts containers** with the specified configuration (ports, environment variables, volumes)

The lifecycle looks like this:

```
docker-compose.yml
        │
        ▼
┌─────────────────┐     ┌──────────────────┐     ┌───────────────────┐
│ Pull Images      │ ──▶ │ Create Network   │ ──▶ │ Create Volumes    │
│ from Docker Hub  │     │ & Volumes        │     │ (if first time)   │
└─────────────────┘     └──────────────────┘     └───────────────────┘
                                                          │
                                                          ▼
                                                 ┌───────────────────┐
                                                 │ Start Containers  │
                                                 │ (PostgreSQL,      │
                                                 │  Redis, RabbitMQ) │
                                                 └───────────────────┘
```

The `-d` flag means **detached mode** — containers run in the background so your terminal stays free.

---

## The docker-compose.yml Explained

Here's the full file, broken down section by section:

### 1. PostgreSQL + pgvector

```yaml
postgres:
  image: ankane/pgvector:v0.5.1
  container_name: cortex_postgres
  environment:
    POSTGRES_USER: cortex
    POSTGRES_PASSWORD: cortex_password
    POSTGRES_DB: cortex_db
  ports:
    - "5432:5432"
  volumes:
    - postgres_data:/var/lib/postgresql/data
  restart: unless-stopped
```

| Property | What it does |
|---|---|
| `image: ankane/pgvector:v0.5.1` | Uses a special PostgreSQL image that comes with the **pgvector** extension pre-installed. pgvector enables storing and querying vector embeddings (used for semantic search). This image is based on PostgreSQL 15. |
| `container_name: cortex_postgres` | Names the container `cortex_postgres` so you can reference it by name (e.g., `docker exec cortex_postgres ...`) |
| `POSTGRES_USER: cortex` | Creates a database user named `cortex` on first startup |
| `POSTGRES_PASSWORD: cortex_password` | Sets the password for the `cortex` user |
| `POSTGRES_DB: cortex_db` | Automatically creates a database named `cortex_db` on first startup |
| `ports: "5432:5432"` | Maps port 5432 inside the container to port 5432 on your Mac. Format: `host_port:container_port`. This is why your app can connect to `localhost:5432`. |
| `volumes: postgres_data:/var/lib/postgresql/data` | Stores database files in a named Docker volume called `postgres_data`. This means **your data persists** even if the container is stopped/removed. |
| `restart: unless-stopped` | Automatically restart the container if it crashes, unless you explicitly stop it with `docker compose stop` |

**How your app connects:**
```
Connection String: "Host=localhost;Port=5432;Database=cortex_db;Username=cortex;Password=cortex_password"
```

**To manually connect via CLI:**
```bash
docker exec -it cortex_postgres psql -U cortex -d cortex_db
```

---

### 2. Redis

```yaml
redis:
  image: redis:7-alpine
  container_name: cortex_redis
  ports:
    - "6379:6379"
  volumes:
    - redis_data:/data
  restart: unless-stopped
```

| Property | What it does |
|---|---|
| `image: redis:7-alpine` | Redis version 7 on Alpine Linux (a tiny ~5MB Linux distro, making the image very small and fast to download) |
| `container_name: cortex_redis` | Names the container `cortex_redis` |
| `ports: "6379:6379"` | Exposes Redis on the default port 6379. Your app connects to `localhost:6379`. |
| `volumes: redis_data:/data` | Persists Redis data (RDB snapshots) to a named volume. Without this, all cached data would be lost on container restart. |
| `restart: unless-stopped` | Auto-restart on crash |

**No password is configured** — Redis runs with default settings (no authentication). This is fine for local development.

**How your app connects:**
```
Redis Connection String: "localhost:6379"
```

**To manually connect via CLI:**
```bash
# Open Redis CLI inside the container
docker exec -it cortex_redis redis-cli

# Once inside, try these commands:
PING              # Should return PONG
SET mykey "hello" # Store a value
GET mykey         # Retrieve it → "hello"
KEYS *            # List all keys
FLUSHALL          # Delete everything (careful!)
EXIT              # Exit the CLI
```

**What Redis does in Cortex:**
- Caches the content feed responses for 5 minutes
- Uses the cache-aside pattern: check Redis first → if miss, query PostgreSQL → store result in Redis
- Cache keys look like: `feed:{userId}:{energyLevel}:{platformType}:{page}`

---

### 3. RabbitMQ

```yaml
rabbitmq:
  image: rabbitmq:3-management-alpine
  container_name: cortex_rabbitmq
  environment:
    RABBITMQ_DEFAULT_USER: cortex
    RABBITMQ_DEFAULT_PASS: cortex_dev
  ports:
    - "5672:5672"    # AMQP protocol port
    - "15672:15672"  # Management UI
  volumes:
    - rabbitmq_data:/var/lib/rabbitmq
  restart: unless-stopped
```

| Property | What it does |
|---|---|
| `image: rabbitmq:3-management-alpine` | RabbitMQ 3 with the **management plugin** enabled (provides a web UI). Alpine-based for small image size. |
| `RABBITMQ_DEFAULT_USER: cortex` | Creates an admin user named `cortex` on first startup |
| `RABBITMQ_DEFAULT_PASS: cortex_dev` | Sets the password for the `cortex` user |
| `ports: "5672:5672"` | **AMQP protocol port** — this is what your app connects to for publishing/consuming messages |
| `ports: "15672:15672"` | **Management UI port** — a web dashboard to monitor queues, messages, connections |
| `volumes: rabbitmq_data:/var/lib/rabbitmq` | Persists queue data, message states, and configuration |

**What RabbitMQ does in Cortex:**
- Acts as a **message broker** between the API and the AI Worker
- When a user saves a URL, the API publishes a message to the `ai_extraction_queue`
- The `AIExtractionWorker` (background service) consumes messages from that queue and processes content through the AI pipeline

```
User saves URL → API publishes message → RabbitMQ queue → AI Worker consumes → Processes content
```

---

### 4. Volumes

```yaml
volumes:
  postgres_data:
  redis_data:
  rabbitmq_data:
```

These are **named Docker volumes** — managed by Docker to persist data on your disk.

| Volume | Purpose | What happens if deleted |
|---|---|---|
| `postgres_data` | All database tables, indexes, data | All DB data is lost — need to re-run migrations |
| `redis_data` | Cached responses (RDB snapshots) | Cache is cleared — app still works, just slower on first request |
| `rabbitmq_data` | Queue state, unprocessed messages | Pending messages are lost |

**Where are volumes stored on disk?**
```bash
# List all Docker volumes
docker volume ls

# Inspect a specific volume to see its mount point
docker volume inspect cortex_postgres_data
```

On macOS with Docker Desktop, volumes are stored inside the Docker VM (not directly accessible on your filesystem).

**To delete all data and start fresh:**
```bash
docker compose down -v    # -v removes volumes too
docker compose up -d      # Recreates everything from scratch
```

---

## How Containers Were Created

When you ran `docker compose up -d`, here's exactly what happened step by step:

### Step 1: Image Pull

Docker downloaded three images from Docker Hub:

```
✓ redis:7-alpine           (~15 MB)  — Pulled first (smallest)
✓ rabbitmq:3-management-alpine (~40 MB)  — Pulled second
✓ ankane/pgvector:v0.5.1   (~100 MB) — Pulled last (largest)
```

These images are now cached locally. Future `docker compose up` calls won't re-download them.

```bash
# See cached images
docker images
```

### Step 2: Network Creation

Docker created an isolated network called `cortex_default`:

```
┌─────────────────── cortex_default network ───────────────────┐
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐  │
│  │cortex_postgres│  │ cortex_redis │  │ cortex_rabbitmq  │  │
│  │   :5432       │  │   :6379      │  │  :5672 / :15672  │  │
│  └──────────────┘  └──────────────┘  └───────────────────┘  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
         │                    │                    │
    port mapping         port mapping         port mapping
         │                    │                    │
    localhost:5432      localhost:6379     localhost:5672/15672
         │                    │                    │
    ┌─────────────── Your Mac (Host Machine) ────────────────┐
    │                                                         │
    │   Cortex.API (dotnet run) connects via localhost ports  │
    └─────────────────────────────────────────────────────────┘
```

Containers within this network can reference each other by service name (e.g., `postgres`, `redis`, `rabbitmq`). Your app running on the host machine connects via `localhost` + mapped ports.

### Step 3: Volume Creation

Three named volumes were created for persistent storage:

```
cortex_postgres_data  → /var/lib/postgresql/data (inside container)
cortex_redis_data     → /data (inside container)
cortex_rabbitmq_data  → /var/lib/rabbitmq (inside container)
```

### Step 4: Container Start

Each container was created and started:

```
Container cortex_postgres  Created → Started
Container cortex_redis     Created → Started
Container cortex_rabbitmq  Created → Started
```

All three start in parallel (they have no dependency on each other).

---

## RabbitMQ Management UI — Login & Usage

RabbitMQ includes a web-based management dashboard for monitoring queues, connections, and messages.

### Access

| Item | Value |
|---|---|
| **URL** | [http://localhost:15672](http://localhost:15672) |
| **Username** | `cortex` |
| **Password** | `cortex_dev` |

### Login Steps

1. Open your browser and navigate to **http://localhost:15672**
2. You'll see the RabbitMQ login page
3. Enter:
   - **Username**: `cortex`
   - **Password**: `cortex_dev`
4. Click **Login**

### Dashboard Overview

Once logged in, you'll see several tabs:

| Tab | What it shows |
|---|---|
| **Overview** | Server stats — message rates, connections, node info |
| **Connections** | Active connections from your Cortex app |
| **Channels** | Open channels within connections |
| **Exchanges** | Message routing points (the default `""` exchange is used by Cortex) |
| **Queues** | The actual queues — look for `ai_extraction_queue` here |
| **Admin** | User management, permissions, policies |

### What to Look For

#### Before any content is saved:
- **Queues tab**: Empty — no queues created yet. The `ai_extraction_queue` is created on-demand when the app first publishes or consumes a message.

#### After content is saved via the API:
- **Queues tab**: You should see `ai_extraction_queue` with:
  - **Ready**: Number of messages waiting to be processed
  - **Unacked**: Messages currently being processed by the AI Worker
  - **Total**: Sum of Ready + Unacked
- **Connections tab**: Should show connections from the Cortex app

#### Useful actions in the UI:
- **Purge a queue**: Delete all messages in a queue (useful for testing)
- **Get messages**: Peek at messages without consuming them
- **Delete a queue**: Remove the queue entirely

### RabbitMQ CLI (inside container)

```bash
# List all queues with message counts
docker exec cortex_rabbitmq rabbitmqctl list_queues

# List all connections
docker exec cortex_rabbitmq rabbitmqctl list_connections

# Check server status
docker exec cortex_rabbitmq rabbitmqctl status

# List users
docker exec cortex_rabbitmq rabbitmqctl list_users
```

### How Cortex Uses RabbitMQ

```
┌────────────────┐         ┌──────────────────────┐         ┌─────────────────────┐
│  API Controller │         │      RabbitMQ         │         │   AI Extraction     │
│  (ContentCtrl)  │         │                      │         │   Worker            │
│                 │         │  ┌────────────────┐  │         │   (BackgroundSvc)   │
│  SaveContent()  │────────▶│  │ai_extraction_  │  │────────▶│                     │
│                 │ publish │  │    queue        │  │ consume │  ProcessContent()   │
│                 │         │  │                 │  │         │  ├─ Extract text    │
│                 │         │  │  [msg1] [msg2]  │  │         │  ├─ AI Summary      │
│                 │         │  │                 │  │         │  ├─ Generate embed   │
│                 │         │  └────────────────┘  │         │  └─ Save to DB       │
└────────────────┘         └──────────────────────┘         └─────────────────────┘
```

**Message format** published to the queue:
```json
{
  "ContentItemId": "guid-here",
  "UserId": "guid-here",
  "Url": "https://youtube.com/watch?v=...",
  "PlatformType": "YouTube"
}
```

**Message delivery guarantees:**
- Queue is **durable** (survives RabbitMQ restart)
- Messages are **persistent** (written to disk)
- Consumer uses **manual acknowledgment** (ack after successful processing, nack+requeue on failure)

---

## Common Docker Commands

### Container Management

```bash
# Start all containers
docker compose up -d

# Stop all containers (keeps data)
docker compose down

# Stop and DELETE all data (fresh start)
docker compose down -v

# Restart all containers
docker compose restart

# Restart a specific service
docker compose restart redis

# View running containers
docker compose ps

# View logs (all services, follow mode)
docker compose logs -f

# View logs for a specific service
docker compose logs -f postgres
docker compose logs -f redis
docker compose logs -f rabbitmq
```

### Executing Commands Inside Containers

```bash
# Open a shell inside a container
docker exec -it cortex_postgres bash
docker exec -it cortex_redis sh        # Alpine uses sh, not bash
docker exec -it cortex_rabbitmq sh

# Run a one-off command
docker exec cortex_redis redis-cli ping
docker exec cortex_postgres psql -U cortex -d cortex_db -c "SELECT version();"
```

### Image Management

```bash
# List downloaded images
docker images

# Remove unused images (free disk space)
docker image prune

# Pull latest versions of images
docker compose pull
```

### Volume Management

```bash
# List all volumes
docker volume ls

# Inspect a volume
docker volume inspect cortex_postgres_data

# Remove all unused volumes (CAUTION: data loss)
docker volume prune
```

### Troubleshooting

```bash
# Check if a container is crashing
docker compose logs --tail=50 postgres

# Check resource usage
docker stats

# Inspect a container's full config
docker inspect cortex_postgres

# Check which ports are mapped
docker port cortex_postgres
docker port cortex_redis
docker port cortex_rabbitmq
```

---

## How Cortex Connects to These Services

All connections happen over **localhost** using the port mappings defined in `docker-compose.yml`:

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Mac (Host)                           │
│                                                             │
│   ┌─────────────────────┐                                   │
│   │   Cortex.API        │                                   │
│   │   (dotnet run)      │                                   │
│   │                     │                                   │
│   │   Connects to:      │                                   │
│   │   • localhost:5432 ──┼──── PostgreSQL (cortex_postgres)  │
│   │   • localhost:6379 ──┼──── Redis (cortex_redis)          │
│   │   • localhost:5672 ──┼──── RabbitMQ (cortex_rabbitmq)    │
│   └─────────────────────┘                                   │
│                                                             │
│   Browser:                                                  │
│   • localhost:5183 ──── Swagger UI (Cortex API)              │
│   • localhost:15672 ─── RabbitMQ Management Dashboard        │
└─────────────────────────────────────────────────────────────┘
```

**Connection is configured in `appsettings.json`:**

| Service | Config Key | Value |
|---|---|---|
| PostgreSQL | `ConnectionStrings:CortexDatabase` | `Host=localhost;Port=5432;Database=cortex_db;Username=cortex;Password=cortex_password` |
| Redis | `Redis:ConnectionString` | `localhost:6379` |
| RabbitMQ | `RabbitMq:HostName` + `Port` | `localhost:5672`, user=`cortex`, pass=`cortex_dev` |
