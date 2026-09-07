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
- Dashboard admin: layout, sidebar, rotas `/dashboard/*` (stubs) e redirect pós-login/onboarding
- FK `users.AcademyId` → `academies.Id` (ON DELETE SET NULL)
- Login/refresh sanitiza `AcademyId` órfão e devolve o usuário para o onboarding
- Stripe SaaS: campos `Plan`, `StripeCustomerId`, `StripeSubscriptionId`, `TrialEndsAt` na academy
- Endpoints: `POST /api/stripe/checkout-session`, `POST /api/stripe/portal-session`, `POST /api/webhooks/stripe`
- Onboarding passo 2 (Starter/Pro) + página `/dashboard/assinatura` (código pronto; `enforceSubscription` desligado no dev)
- Perfil admin `/dashboard/perfil`: dados do admin, senha, academia e link para assinatura
- Auth: `PATCH /api/auth/profile`, `POST /api/auth/change-password`
- Domínio Students: entity expandida + `student_sports`, enroll transacional, soft-deactivate
- APIs: `GET/POST/PUT /api/students`, enroll, activate/deactivate
- UI: `/dashboard/alunos` (lista, novo wizard, editar)
- ViaCEP: `GET /api/viacep?cep=` + autofill no cadastro/edição de aluno

## [2.0.0-auth] - 2026-08-31

### Added

- Auth JWT: ASP.NET Identity + login, register, refresh, logout
- Tabelas `users`, `roles`, `refresh_tokens` (migration AddIdentityAuth)
- Tela de login Angular + interceptor Bearer
- Swagger com autenticação JWT

## [2.0.0-docker] - 2026-08-31

## [2.0.0-scaffold] - 2026-08-30
