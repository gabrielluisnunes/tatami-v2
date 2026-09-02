# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).
Versionamento baseado em [Semantic Versioning](https://semver.org/lang/pt-BR/).

## [Unreleased]

### Added

- Domínio Academy expandido (owner, sport, monthly price, subscription status)
- Onboarding: `POST /api/onboarding` (admin sem academia)
- Academies: `GET /api/academies/me`, `PUT /api/academies/me`
- Migration `ExpandAcademyAndOnboarding`
- Tela Angular `/onboarding` + guards de fluxo pós-login

## [2.0.0-auth] - 2026-08-31

### Added

- Auth JWT: ASP.NET Identity + login, register, refresh, logout
- Tabelas `users`, `roles`, `refresh_tokens` (migration AddIdentityAuth)
- Tela de login Angular + interceptor Bearer
- Swagger com autenticação JWT

## [2.0.0-docker] - 2026-08-31

## [2.0.0-scaffold] - 2026-08-30
