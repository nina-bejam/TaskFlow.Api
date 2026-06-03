# TaskFlow.Api

A local-first .NET 8 Minimal API for project and task management with JWT authentication, file attachments, and optional AI-powered task suggestions via Ollama.

Everything runs on your machine. No paid cloud services, API keys, or vendor lock-in required.

---

## What is TaskFlow.Api?

TaskFlow.Api is a backend service that lets users register an account, organize work into projects, track tasks, upload files, and optionally generate task ideas using a local language model.

It is designed as a practical, portfolio-ready example of modern backend development: clean structure, real persistence, secure auth, and a fully local AI workflow — all runnable with Docker Compose or the .NET SDK alone.

---

## Architecture

```
┌─────────────┐     JWT      ┌──────────────────────────────────────┐
│   Client    │─────────────▶│           TaskFlow.Api               │
│ (Swagger,   │              │                                      │
│  curl, etc) │              │  Endpoints → AppDbContext            │
└─────────────┘              │           → LocalFileStorage         │
                             │           → OllamaService            │
                             └──────┬──────────────┬────────────────┘
                                    │              │
                         ┌──────────▼───┐   ┌──────▼──────┐
                         │ SQLite /     │   │  uploads/   │
                         │ PostgreSQL   │   │  (local)    │
                         └──────────────┘   └─────────────┘
                                    │
                             ┌──────▼──────┐
                             │   Ollama    │
                             │ llama3.2 /  │
                             │   mistral   │
                             └─────────────┘
```

The application follows a simple layered layout:

| Layer | Role |
|---|---|
| **Endpoints** | Minimal API route groups for auth, projects, tasks, files, and AI |
| **DTOs** | Request and response contracts decoupled from domain models |
| **Services** | JWT issuance, password hashing, file storage, and Ollama integration |
| **Data** | EF Core `AppDbContext` with automatic migrations on startup |
| **Models** | Users, projects, tasks, and file metadata |

User data is isolated per account. Every protected endpoint validates the JWT and scopes queries to the authenticated user.

---

## Features

- **User authentication** — register and login with custom JWT tokens (PBKDF2 password hashing)
- **Project management** — full CRUD for projects owned by the logged-in user
- **Task management** — full CRUD for tasks within a project, including completion status
- **File attachments** — upload, download, and delete files stored on the local filesystem
- **AI task suggestions** — ask Ollama to propose actionable task titles for a new project
- **OpenAPI documentation** — interactive Swagger UI with JWT bearer support
- **Dual database support** — SQLite for quick local runs, PostgreSQL in Docker
- **Zero-cost stack** — no OpenAI, Azure, AWS, Auth0, Firebase, or other paid dependencies

---

## Technologies used

| Category | Technology |
|---|---|
| Runtime | .NET 8 |
| API style | ASP.NET Core Minimal API |
| ORM | Entity Framework Core 8 |
| Database | SQLite (local) / PostgreSQL 16 (Docker) |
| Authentication | JWT Bearer (custom implementation) |
| API docs | Swashbuckle (Swagger / OpenAPI) |
| AI | Ollama (Llama 3.2 or Mistral) |
| File storage | Local filesystem |
| Containerization | Docker + Docker Compose |

---

## Local AI with Ollama

TaskFlow uses [Ollama](https://ollama.com) to run language models locally. The `POST /api/ai/suggest-tasks` endpoint sends a project name and description to Ollama and returns a list of suggested task titles.

Supported models:

- **llama3.2** (default)
- **mistral**

### Pull a model

**Docker:**

```bash
docker compose exec ollama ollama pull llama3.2
```

**Local Ollama install:**

```bash
ollama pull llama3.2
```

To switch models, set `OLLAMA_MODEL=mistral` in your `.env` (Docker) or update `Ollama:Model` in `appsettings.json` (local run), then pull the matching model.

The rest of the API works without Ollama. If the model is unavailable, the AI endpoint returns `503 Service Unavailable`.

---

## Docker setup

The recommended way to run the full stack is Docker Compose. It starts three services: the API, PostgreSQL, and Ollama.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine + Compose
- Git

### Steps

```bash
git clone https://github.com/<your-org>/TaskFlow.Api.git
cd TaskFlow.Api
cp .env.example .env
docker compose up --build
```

Pull the AI model after the stack is running:

```bash
docker compose exec ollama ollama pull llama3.2
```

### Services

| Service | Description | Address |
|---|---|---|
| `api` | TaskFlow Minimal API + Swagger | http://localhost:5000 |
| `postgres` | PostgreSQL database | `localhost:5432` |
| `ollama` | Local LLM runtime | http://localhost:11434 |

### Useful commands

```bash
# Stop containers
docker compose down

# Stop and remove all persisted data
docker compose down -v

# View API logs
docker compose logs api
```

Database migrations run automatically when the API container starts.

---

## Environment variables

Copy `.env.example` to `.env` to customize settings. Docker Compose reads `.env` automatically.

```bash
cp .env.example .env
```

| Variable | Default | Description |
|---|---|---|
| `API_PORT` | `5000` | Port exposed by the API |
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET Core environment |
| `POSTGRES_USER` | `taskflow` | PostgreSQL username |
| `POSTGRES_PASSWORD` | `taskflow` | PostgreSQL password |
| `POSTGRES_DB` | `taskflow` | PostgreSQL database name |
| `POSTGRES_PORT` | `5432` | PostgreSQL host port |
| `OLLAMA_PORT` | `11434` | Ollama host port |
| `OLLAMA_MODEL` | `llama3.2` | Model name sent to Ollama |
| `JWT_KEY` | dev key | Secret used to sign JWT tokens |
| `JWT_ISSUER` | `TaskFlow.Api` | JWT issuer claim |
| `JWT_AUDIENCE` | `TaskFlow.Api` | JWT audience claim |
| `FILE_STORAGE_PATH` | `/app/uploads` | Upload directory inside the container |

For local runs without Docker, settings are in `appsettings.json`:

| Key | Default |
|---|---|
| `ConnectionStrings:DefaultConnection` | `Data Source=taskflow.db` |
| `Jwt:Key` | dev key |
| `Ollama:BaseUrl` | `http://localhost:11434` |
| `Ollama:Model` | `llama3.2` |
| `FileStorage:Path` | `uploads` |

---

## How to run locally

### Option 1 — Docker Compose (full stack)

```bash
cp .env.example .env
docker compose up --build
docker compose exec ollama ollama pull llama3.2
```

### Option 2 — .NET SDK (lightweight)

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run
```

This uses SQLite (`taskflow.db` is created automatically) and serves the API on port `5080`. Install Ollama separately if you want AI features.

### First-time workflow

1. Open Swagger (see URLs below)
2. Register a user via `POST /api/auth/register`
3. Copy the JWT from the response
4. Click **Authorize** in Swagger and enter `Bearer <your-token>`
5. Create a project, add tasks, upload files, or try AI suggestions

---

## Swagger URL

| Environment | URL |
|---|---|
| Docker Compose | http://localhost:5000/swagger |
| Local `dotnet run` | http://localhost:5080/swagger |

The root path (`/`) redirects to Swagger in both environments.

---

## Example API calls

### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com"
}
```

### Create a project

```http
POST /api/projects
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Website redesign",
  "description": "Homepage and landing pages for Q2"
}
```

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Website redesign",
  "description": "Homepage and landing pages for Q2",
  "createdAt": "2026-06-03T19:00:00Z"
}
```

### Create a task

```http
POST /api/projects/{projectId}/tasks
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Update hero section",
  "description": "New copy and layout"
}
```

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Update hero section",
  "description": "New copy and layout",
  "isCompleted": false,
  "createdAt": "2026-06-03T19:05:00Z"
}
```

### Upload a file

```http
POST /api/projects/{projectId}/tasks/{taskId}/files
Authorization: Bearer <token>
Content-Type: multipart/form-data

file: <your-file>
```

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "taskId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "fileName": "mockup.png",
  "contentType": "image/png",
  "sizeBytes": 245760,
  "createdAt": "2026-06-03T19:10:00Z"
}
```

### Get AI task suggestions

```http
POST /api/ai/suggest-tasks
Authorization: Bearer <token>
Content-Type: application/json

{
  "projectName": "Website redesign",
  "description": "Homepage and landing pages for Q2"
}
```

```json
{
  "suggestions": [
    "Audit current homepage content",
    "Create wireframes for new hero section",
    "Write updated landing page copy",
    "Implement responsive layout",
    "Run cross-browser QA pass"
  ]
}
```

---

## Project structure

```
TaskFlow.Api/
├── Data/              # EF Core DbContext
├── DTOs/              # Request/response records
├── Endpoints/         # Minimal API route groups
├── Extensions/        # Helper extensions
├── Migrations/        # EF Core migrations
├── Models/            # Domain entities
├── Services/          # JWT, passwords, files, Ollama
├── Properties/        # Launch settings
├── .env.example       # Docker environment template
├── appsettings.json
├── Dockerfile
└── docker-compose.yml
```

---

## Future roadmap

Planned improvements that build on the current foundation without introducing paid dependencies:

- **Task priorities and due dates** — extend the task model with scheduling fields
- **Project tags and filtering** — organize and search projects more easily
- **Refresh tokens** — longer-lived sessions without storing plain passwords
- **Role-based access** — shared projects with owner and member roles
- **Structured AI output** — return parsed JSON from Ollama instead of plain text lines
- **Integration tests** — automated coverage for auth, CRUD, and file upload flows
- **CI pipeline** — GitHub Actions build and test on every pull request
- **Health check endpoints** — readiness probes for Docker and Kubernetes deployments

---

## License

MIT — see [LICENSE](LICENSE).
