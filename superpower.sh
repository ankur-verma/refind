#!/bin/bash

# Configuration and Paths
WORKSPACE_DIR="/Users/apple/Desktop/refind/refind"
BACKEND_DIR="$WORKSPACE_DIR/src/Cortex"
FRONTEND_DIR="$WORKSPACE_DIR/frontend"

PID_DIR="$WORKSPACE_DIR/bin"
BACKEND_PID_FILE="$PID_DIR/cortex-backend.pid"
FRONTEND_PID_FILE="$PID_DIR/cortex-frontend.pid"

# Set up the toolchain PATH
export PATH="$PATH:/usr/local/bin:/usr/local/share/dotnet"

# Helper print functions
log_info() { echo -e "\033[0;32m[INFO]\033[0m $1"; }
log_warn() { echo -e "\033[0;33m[WARN]\033[0m $1"; }
log_err()  { echo -e "\033[0;31m[ERROR]\033[0m $1"; }

mkdir -p "$PID_DIR"
mkdir -p "$BACKEND_DIR/logs"
mkdir -p "$FRONTEND_DIR/logs"

status() {
    local backend_running=false
    local frontend_running=false

    if lsof -i :5183 >/dev/null; then backend_running=true; fi
    if lsof -i :5173 >/dev/null; then frontend_running=true; fi

    echo "=== Server Status ==="
    if [ "$backend_running" = true ]; then
        log_info "Backend (.NET API): Running on http://localhost:5183 (Swagger: http://localhost:5183/swagger)"
    else
        log_warn "Backend (.NET API): Stopped"
    fi

    if [ "$frontend_running" = true ]; then
        log_info "Frontend (Vite/React): Running on http://localhost:5173"
    else
        log_warn "Frontend (Vite/React): Stopped"
    fi
}

build() {
    log_info "Building Backend..."
    dotnet build "$BACKEND_DIR/Cortex.csproj"
    if [ $? -ne 0 ]; then
        log_err "Backend build failed!"
        exit 1
    fi
    log_info "Backend build succeeded."

    log_info "Preparing Frontend..."
    cd "$FRONTEND_DIR"
    if [ ! -d "node_modules" ]; then
        log_info "Installing npm dependencies..."
        npm install
    fi
    log_info "Building Frontend assets..."
    npm run build
    if [ $? -ne 0 ]; then
        log_err "Frontend build failed!"
        exit 1
    fi
    log_info "Frontend prepared."
}

start() {
    if lsof -i :5183 >/dev/null; then
        log_warn "Backend is already running on port 5183."
    else
        log_info "Starting Backend server..."
        cd "$BACKEND_DIR"
        ASPNETCORE_ENVIRONMENT=Development dotnet run > logs/cortex-run.log 2>&1 &
        echo $! > "$BACKEND_PID_FILE"
        sleep 2
        if lsof -i :5183 >/dev/null; then
            log_info "Backend started (PID $(cat "$BACKEND_PID_FILE"))."
        else
            log_err "Backend failed to start. Check logs at src/Cortex/logs/cortex-run.log"
        fi
    fi

    if lsof -i :5173 >/dev/null; then
        log_warn "Frontend is already running on port 5173."
    else
        log_info "Starting Frontend server..."
        cd "$FRONTEND_DIR"
        npm run dev > logs/frontend-run.log 2>&1 &
        echo $! > "$FRONTEND_PID_FILE"
        sleep 2
        if lsof -i :5173 >/dev/null; then
            log_info "Frontend started (PID $(cat "$FRONTEND_PID_FILE"))."
        else
            log_err "Frontend failed to start. Check logs at frontend/logs/frontend-run.log"
        fi
    fi
}

stop() {
    log_info "Stopping servers..."
    if [ -f "$BACKEND_PID_FILE" ]; then
        local pid=$(cat "$BACKEND_PID_FILE")
        if ps -p $pid > /dev/null; then
            log_info "Killing backend PID $pid..."
            kill $pid 2>/dev/null
        fi
        rm -f "$BACKEND_PID_FILE"
    fi

    # Fallback to killing whatever is on port 5183
    local port_pid=$(lsof -ti :5183)
    if [ ! -z "$port_pid" ]; then
        log_info "Killing backend process on port 5183 (PIDs: $port_pid)..."
        kill -9 $port_pid 2>/dev/null
    fi

    if [ -f "$FRONTEND_PID_FILE" ]; then
        local pid=$(cat "$FRONTEND_PID_FILE")
        if ps -p $pid > /dev/null; then
            log_info "Killing frontend PID $pid..."
            kill $pid 2>/dev/null
        fi
        rm -f "$FRONTEND_PID_FILE"
    fi

    # Fallback to killing whatever is on port 5173
    local port_pid_fe=$(lsof -ti :5173)
    if [ ! -z "$port_pid_fe" ]; then
        log_info "Killing frontend process on port 5173 (PIDs: $port_pid_fe)..."
        kill -9 $port_pid_fe 2>/dev/null
    fi

    log_info "Servers stopped."
}

db_migrate() {
    if [ -z "$1" ]; then
        log_err "Usage: ./superpower.sh db-migrate <migration_name>"
        exit 1
    fi
    log_info "Adding DB Migration: $1..."
    dotnet ef migrations add "$1" --project "$BACKEND_DIR/Cortex.csproj"
    if [ $? -eq 0 ]; then
        log_info "Applying DB Migration..."
        dotnet ef database update --project "$BACKEND_DIR/Cortex.csproj"
    else
        log_err "Migration generation failed!"
    fi
}

logs() {
    local target=${1:-backend}
    if [ "$target" = "frontend" ]; then
        tail -n 50 -f "$FRONTEND_DIR/logs/frontend-run.log"
    else
        tail -n 50 -f "$BACKEND_DIR/logs/cortex-run.log"
    fi
}

case "$1" in
    start)
        start
        ;;
    stop)
        stop
        ;;
    status)
        status
        ;;
    build)
        build
        ;;
    db-migrate)
        db_migrate "$2"
        ;;
    logs)
        logs "$2"
        ;;
    *)
        echo "Usage: ./superpower.sh {start|stop|status|build|db-migrate <name>|logs [backend|frontend]}"
        exit 1
        ;;
esac
