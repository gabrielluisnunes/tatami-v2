# Tatami v2

SaaS de gestão de academias de artes marciais — reescrita em **ASP.NET Core 8**, **Angular 19** e **infra self-hosted** (VPS).

Repositório v1 (Next.js + Supabase): [tatami](https://github.com/gabrielluisnunes/tatami)

## Stack

| Camada | Tecnologia |
|--------|------------|
| Backend | ASP.NET Core 8 (Clean Architecture) |
| Frontend | Angular 19 |
| Banco | PostgreSQL (EF Core) — issue #2 |
| Auth | ASP.NET Identity + JWT — issue #3 |
| Storage | MinIO — issue #2 |
| Cache | Redis — issue #2 |
| Deploy | VPS + Docker — issues #6–#9 |

## Estrutura

```
tatami-v2/
├── Tatami.sln
├── src/
│   ├── Tatami.Domain/          # entidades e regras puras
│   ├── Tatami.Application/     # use cases / services
│   ├── Tatami.Infrastructure/  # banco, email, stripe, storage
│   ├── Tatami.Api/             # controllers, middleware
│   └── tatami-web/             # Angular
├── infra/                      # docker, nginx, scripts
└── docs/                       # arquitetura e runbooks
```

## Desenvolvimento local

### Pré-requisitos

- .NET SDK 8+
- Node.js 20+
- Docker Desktop (a partir da issue #2)
- JetBrains Rider (backend) + Cursor (frontend/agente)

### Backend

```bash
cd src/Tatami.Api
dotnet run
```

API em `https://localhost:7xxx` (ver `launchSettings.json`).

Health check: `GET /health`

### Frontend

```bash
cd src/tatami-web
npm install
npm start
```

App em `http://localhost:4200`

### Solution completa (Rider)

Abra `Tatami.sln` na raiz do repositório.

## Branches

```
feature/* → develop → main
```

CI/CD será configurado na issue #9.

## Milestone

**v2.0.0** — Reescrita C# + Angular + VPS
