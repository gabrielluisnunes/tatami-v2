# Docker — ambiente local

## Pré-requisitos

- Docker Desktop em execução

## Subir serviços

Na raiz do repositório:

```bash
cp .env.example .env
docker compose -f infra/docker/docker-compose.yml --env-file .env up -d
```

## Serviços

| Serviço    | Porta | Uso                          |
|------------|-------|------------------------------|
| PostgreSQL | 5433  | Banco principal (EF Core) — porta 5433 evita conflito com Postgres do Mac |
| Redis      | 6379  | Rate limiting (issue #11)    |
| MinIO      | 9000  | API S3-compatible            |
| MinIO UI   | 9001  | Console web                  |

Credenciais padrão: ver `.env.example`.

## Comandos úteis

```bash
# Status
docker compose -f infra/docker/docker-compose.yml ps

# Logs
docker compose -f infra/docker/docker-compose.yml logs -f postgres

# Parar (mantém volumes)
docker compose -f infra/docker/docker-compose.yml down

# Parar e apagar volumes (cuidado: apaga dados locais)
docker compose -f infra/docker/docker-compose.yml down -v
```

## Migrations (EF Core)

Com Postgres rodando:

```bash
dotnet ef database update \
  --project src/Tatami.Infrastructure \
  --startup-project src/Tatami.Api
```

## MinIO console

Abra `http://localhost:9001` e entre com `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`.
