# Arquitetura — Tatami v2

## Visão geral

```
Angular (tatami-web)
        │ HTTP / JSON
        ▼
   Tatami.Api              ← controllers, auth, rate limit
        │
        ▼
 Tatami.Application         ← use cases (CreateStudent, etc.)
        │
        ▼
   Tatami.Domain            ← entidades, interfaces, regras puras
        ▲
        │ implementa
 Tatami.Infrastructure      ← EF Core, Stripe, MinIO, Redis, email
```

## Projetos

### Tatami.Domain

Sem dependências externas. Contém:

- **Entities** — `Student`, `Academy`, etc.
- **Interfaces** — contratos de repositórios e serviços externos
- **Common** — `BaseEntity`, value objects, enums

### Tatami.Application

Depende de **Domain**. Contém:

- **Services / Use Cases** — orquestram a lógica de negócio
- **DependencyInjection** — registra services na API

### Tatami.Infrastructure

Depende de **Application** e **Domain**. Contém:

- **Persistence** — EF Core, `TatamiDbContext`, migrations, repositórios
- **Integrations** — Stripe, Resend, MinIO
- **DependencyInjection** — registra infra na API

Banco local: PostgreSQL via Docker (`infra/docker/docker-compose.yml`).

### Tatami.Api

Depende de **Application** e **Infrastructure**. Contém:

- **Controllers** — endpoints REST
- **Middleware** — JWT, rate limiting, CORS
- **Program.cs** — composição da aplicação

### tatami-web (Angular)

Estrutura feature-based (a expandir):

```
src/app/
├── core/       # auth, interceptors, guards
├── shared/     # componentes reutilizáveis
└── features/   # dashboard, aluno, professor
```

## Fluxo de uma requisição

```
POST /api/students
  → StudentsController
  → CreateStudentService (Application)
  → IStudentRepository (Domain interface)
  → StudentRepository (Infrastructure → PostgreSQL)
```

## Infra (pastas)

| Pasta | Conteúdo |
|-------|----------|
| `infra/docker/` | docker-compose dev e prod |
| `infra/nginx/` | reverse proxy |
| `infra/scripts/` | backup, deploy |

Configurado a partir da **issue #2** (Docker local) e **issue #6** (VPS).

## Referência v1

| v1 | v2 |
|----|-----|
| `app/api/*.ts` | `Tatami.Api` |
| `lib/services/` | `Tatami.Application` |
| `lib/repositories/` | `Tatami.Infrastructure` |
| `types/index.ts` | `Tatami.Domain` |
| `app/dashboard/` | `tatami-web/features/dashboard/` |
