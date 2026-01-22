# Docker Setup for SignalFlow

This document describes how to use Docker Compose to run PostgreSQL for the SignalFlow application.

## Prerequisites

- Docker installed on your system
- Docker Compose installed (usually comes with Docker Desktop)

## Services

The `docker-compose.yml` file includes the following services:

### PostgreSQL
- **Image**: postgres:16-alpine
- **Port**: 5432 (mapped to host port 5432)
- **Database**: signalflow
- **User**: signalflow
- **Password**: signalflow123

### pgAdmin (Database Management UI)
- **Image**: dpage/pgadmin4:latest
- **Port**: 5050 (mapped to host port 5050)
- **Email**: admin@signalflow.com
- **Password**: admin

## Quick Start

### 1. Start the services

```bash
docker-compose up -d
```

This will:
- Pull the necessary Docker images (if not already available)
- Create and start the PostgreSQL and pgAdmin containers
- Create a named volume for PostgreSQL data persistence

### 2. Verify the services are running

```bash
docker-compose ps
```

You should see both `signalflow-postgres` and `signalflow-pgadmin` containers running.

### 3. Check logs (if needed)

```bash
# View all logs
docker-compose logs

# View PostgreSQL logs
docker-compose logs postgres

# Follow logs in real-time
docker-compose logs -f
```

## Accessing the Database

### Using psql (command line)

```bash
docker-compose exec postgres psql -U signalflow -d signalflow
```

### Using pgAdmin (Web UI)

1. Open your browser and navigate to: `http://localhost:5050`
2. Login with:
   - **Email**: admin@signalflow.com
   - **Password**: admin
3. Add a new server connection:
   - **Host**: postgres (this is the service name in docker-compose)
   - **Port**: 5432
   - **Database**: signalflow
   - **Username**: signalflow
   - **Password**: signalflow123

### Connection String for .NET Application

```
Host=localhost;Port=5432;Database=signalflow;Username=signalflow;Password=signalflow123
```

If connecting from within a Docker container in the same network:
```
Host=postgres;Port=5432;Database=signalflow;Username=signalflow;Password=signalflow123
```

## Managing the Services

### Stop the services

```bash
docker-compose stop
```

### Start stopped services

```bash
docker-compose start
```

### Restart the services

```bash
docker-compose restart
```

### Stop and remove containers

```bash
docker-compose down
```

### Stop and remove containers AND volumes (data will be lost)

```bash
docker-compose down -v
```

## Environment Variables

You can customize the database credentials by creating a `.env` file in the same directory as `docker-compose.yml`. Use `.env.example` as a template:

```bash
cp .env.example .env
```

Then edit `.env` with your preferred values.

## Health Check

The PostgreSQL service includes a health check that verifies the database is ready to accept connections. Other services that depend on PostgreSQL will wait for it to be healthy before starting.

## Data Persistence

Database data is stored in a Docker volume named `postgres_data`. This ensures your data persists even if you stop or remove the containers.

To view volumes:
```bash
docker volume ls
```

To inspect the volume:
```bash
docker volume inspect signalflow_postgres_data
```

## Troubleshooting

### Port already in use

If port 5432 or 5050 is already in use, you can change the port mapping in `docker-compose.yml`:

```yaml
ports:
  - "5433:5432"  # Use port 5433 on host instead
```

### Cannot connect to database

1. Check if the container is running: `docker-compose ps`
2. Check the logs: `docker-compose logs postgres`
3. Verify the health check: `docker inspect signalflow-postgres | grep -A 10 Health`

### Reset the database

To completely reset the database, stop the containers and remove the volume:

```bash
docker-compose down -v
docker-compose up -d
```

**Warning**: This will delete all data in the database.
