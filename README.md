# fiap-esperanca-solidaria-campanha-api

API de **campanhas e doações** da plataforma Esperança Solidária (FIAP 11NETT). Gestores de ONG criam,
editam e cancelam campanhas de arrecadação; doadores listam campanhas ativas e fazem doações, que são
enfileiradas no SQS para o `doacao-work` processar o pagamento.

> Para subir o ambiente completo (k8s, LocalStack, API Gateway, front), siga o README do repositório
> **`fiap-esperanca-solidaria-infra`**. Este documento cobre a API isoladamente.

---

## Sumário

- [Stack](#stack)
- [Arquitetura e fluxo](#arquitetura-e-fluxo)
- [Estrutura da solução](#estrutura-da-solução)
- [Endpoints](#endpoints)
- [Regras de domínio](#regras-de-domínio)
- [Configuração](#configuração)
- [Rodando localmente](#rodando-localmente)
- [Rodando no Kubernetes](#rodando-no-kubernetes)
- [Testes](#testes)
- [Servidor MCP](#servidor-mcp)
- [CI/CD](#cicd)
- [Troubleshooting](#troubleshooting)

---

## Stack

| Tecnologia | Uso |
|---|---|
| .NET 10 / ASP.NET Core | API REST (controllers) |
| MediatR + FluentValidation | CQRS (commands/queries) e validação via pipeline behaviors |
| Entity Framework Core + Npgsql | Persistência em PostgreSQL (schema `fundraising`), migrations aplicadas no startup |
| Redis | Cache das consultas (`CachingBehavior`) |
| Hangfire (storage PostgreSQL) | Job diário de atualização de status das campanhas |
| MassTransit / AWS SDK SQS | Publicação da mensagem de doação criada |
| AWS SDK S3 | Upload da imagem de capa da campanha (LocalStack em dev) |
| Firebase JWT | Autenticação (Bearer); papéis na claim `roles` |
| Serilog + OpenTelemetry | Logs estruturados e traces (Tempo) |
| OpenAPI + Scalar | Documentação interativa em `/docs` |
| xUnit | Testes |

---

## Arquitetura e fluxo

```
Front / API Gateway
        │  Bearer <idToken Firebase>
        ▼
┌──────────────────────── campanha-api ────────────────────────┐
│ Controllers ─▶ MediatR (Validation → Caching → Handler)      │
│                    │                │                        │
│                    ▼                ▼                        │
│            PostgreSQL (EF)      Redis (cache)                │
│                    │                                         │
│  CreateDonation ───┴─▶ SQS "process-donation-payment" ───────┼──▶ doacao-work
│  UploadImage ─────────▶ S3 "campanha-images"                 │
│  Hangfire (diário) ───▶ UpdateCampaignStatuses               │
└──────────────────────────────────────────────────────────────┘
```

1. `POST /api/v1/Donation` valida a campanha (precisa estar ativa), grava a doação como `Pending` e
   publica `{ DonationId, CorrelationId }` na fila de doações.
2. O `doacao-work` consome, simula o pagamento e atualiza status da doação e `TotalRaised` da campanha
   no mesmo banco.
3. O job Hangfire `UpdateCampaignStatusesJob` roda diariamente e move campanhas entre `Scheduled`,
   `Active` e `Completed` conforme as datas.

---

## Estrutura da solução

Solução: `src/FiapEsperancaSolidaria.Campanha.slnx`

```
src/
├── FiapEsperancaSolidaria.Campanha.Api/            # Host ASP.NET Core
│   ├── Controllers/                                # CampaignController, DonationController
│   ├── Configurations/                             # Auth (Firebase + DevBypass), CORS, Swagger/Scalar, Health, Jobs, Migrations
│   ├── Program.cs                                  # Pipeline da aplicação
│   ├── appsettings.json                            # Configuração base
│   ├── appsettings.Development.json                # Dev local (LocalStack, Postgres 5444, DevBypass)
│   └── appsettings.Example.json                    # Modelo com placeholders
├── FiapEsperancaSolidaria.Campanha.Application/    # Casos de uso (CQRS)
│   ├── Behaviors/                                  # ValidationBehavior, CachingBehavior
│   ├── DTOs/
│   ├── Features/CampaignFeature/                   # Create/Update/Cancel/UploadImage/UpdateStatuses + queries
│   ├── Features/DonationFeature/                   # CreateDonation + GetById + ListMyDonations
│   └── Jobs/UpdateCampaignStatusesJob.cs
├── FiapEsperancaSolidaria.Campanha.Domain/         # Agregados Campaign e Payment (Donation), enums, contratos, exceções
├── FiapEsperancaSolidaria.Campanha.Infrastructure/ # EF Core (AppDbContext, migrations), repositórios, Redis, S3, Hangfire
├── FiapEsperancaSolidaria.Campanha.Queue/          # MassTransit/SQS e DonationCreatedNotification
├── FiapEsperancaSolidaria.Campanha.Observability/  # Serilog, OpenTelemetry, CorrelationId e Exception middlewares
├── FiapEsperancaSolidaria.Campanha.Mcp/            # Servidor MCP (stdio) que expõe a API como ferramentas
└── FiapEsperancaSolidaria.Campanha.Tests/          # Testes unitários
docker/dockerfile                                   # Build multi-stage da imagem
insomnia/, postman/                                 # Coleções de requisições
.github/workflows/                                  # CI (push/PR) e CD (tag)
```

### Arquivos importantes

- **`Api/Program.cs`** — ordem do pipeline: Serilog → migrations → OpenAPI/Scalar → CorrelationId e
  tratamento global de exceções → `UseHttpsRedirection` → CORS → autenticação/autorização → controllers,
  health checks e dashboard Hangfire.
- **`Api/Configurations/AuthConfig.cs`** — valida tokens do Firebase (`Firebase:ProjectId`), mapeando
  papéis a partir da claim `roles`. Em `Development` com `Auth:DevBypassEnabled=true`, aceita também o
  header `X-Dev-Role` (ver `DevAuthHandler.cs`) — nunca habilitado em outros ambientes.
- **`Infrastructure/Data/AppDbContext.cs`** — schema padrão `fundraising`; as tabelas `Campaigns`,
  `Donation` e `PaymentEvent` são compartilhadas com o `doacao-work`, que **não tem migrations próprias**.
  Qualquer mudança de schema nasce aqui.
- **`Queue/Notifications/DonationCreatedNotification`** — publica na URL configurada em
  `SqsSettings:EmailQueueUrl`. Apesar do nome herdado, esse valor é a fila de **doações**
  (`process-donation-payment`), não de e-mail.
- **`Infrastructure/Storage/S3ImageStorageService.cs`** — cria o bucket se não existir e devolve a URL
  pública usando `S3Settings:PublicBaseUrl` (em k8s, `http://localhost:30466`, que o navegador resolve).

---

## Endpoints

Base direta: `http://localhost:5054` (local) ou `http://localhost:30081` (k8s). Via API Gateway as rotas
são expostas em português (`/api/v1/campanhas`, `/api/v1/doacoes`) — ver o README da infra.

### Campanhas — `api/v1/Campaign`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/Campaign/public` | anônimo | Campanhas visíveis ao público (ativas) com total arrecadado |
| GET | `/api/v1/Campaign` | GestorONG | Todas as campanhas (painel do gestor) |
| GET | `/api/v1/Campaign/{id}` | anônimo | Detalhe de uma campanha |
| POST | `/api/v1/Campaign` | GestorONG | Cria campanha (`title`, `description`, `startDate`, `endDate`, `financialGoal`, `image?`) |
| PUT | `/api/v1/Campaign/{id}` | GestorONG | Atualiza campanha |
| POST | `/api/v1/Campaign/images` | GestorONG | Upload da imagem de capa (multipart) → devolve a URL |
| POST | `/api/v1/Campaign/{id}/cancel` | GestorONG | Cancela a campanha |

### Doações — `api/v1/Donation`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| POST | `/api/v1/Donation` | Doador | Cria doação (`campaignId`, `amount`, `paymentMethod`) → status `Pending` |
| GET | `/api/v1/Donation/me` | Doador | Doações do usuário logado |
| GET | `/api/v1/Donation/{id}` | autenticado | Detalhe de uma doação |

`paymentMethod`: `1` CreditCard, `2` DebitCard, `3` Pix, `4` Boleto.

### Infra

| Rota | Descrição |
|---|---|
| `/docs` | Scalar (OpenAPI) |
| `/health`, `/health/ready`, `/health/live` | Health checks |
| `/hangfire` | Dashboard do Hangfire |

Coleções prontas: `insomnia/campanha-api.insomnia.json` e `postman/campanha-api.postman_collection.json`.

---

## Regras de domínio

- **Status da campanha** (`CampaignStatus`): `Active = 1`, `Completed = 2`, `Cancelled = 3`,
  `Scheduled = 4` (adicionado no fim para não renumerar valores já persistidos).
- **Status da doação** (`DonationStatus`): `Pending → PaymentProcessing → Approved | Rejected`. Só a API
  cria (`Pending`); as transições seguintes são do `doacao-work`.
- Só é possível doar para campanhas ativas; campanhas canceladas/encerradas não aceitam doações.
- `TotalRaised` só é incrementado por doações aprovadas (feito pelo worker).

---

## Configuração

| Chave | Descrição | Exemplo (dev) |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL | `Host=localhost;Port=5444;Database=...` |
| `ConnectionStrings:Redis` | Redis | `localhost:6379,password=...,abortConnect=false` |
| `Firebase:ProjectId` | Projeto Firebase que emite os tokens | `esperancasolidaria` |
| `Auth:DevBypassEnabled` | Aceita `X-Dev-Role` (só em Development) | `true` |
| `Cors:AllowedOrigins` | Origens liberadas | `["http://localhost:5173"]` |
| `SqsSettings:Region/AccessKey/SecretKey/ServiceUrl` | SQS (LocalStack) | `us-east-1` / `test` / `test` / `http://localhost:4566` |
| `SqsSettings:EmailQueueUrl` | **Fila de doações** | `.../000000000000/process-donation-payment` |
| `S3Settings:*` | S3 (LocalStack) + `BucketName` e `PublicBaseUrl` | `campanha-images` |
| `OpenTelemetry:ServiceName` / `TempoEndpoint` | Traces | `campanha-api` / `http://tempo...:4318/v1/traces` |
| `MassTransitSettings:RetryCount/Interval` | Retentativas de publicação | `3` / `3000` |

Em variáveis de ambiente use `__` como separador (ex.: `SqsSettings__ServiceUrl`). `appsettings.Example.json`
serve de modelo; não commite credenciais reais.

---

## Rodando localmente

Pré-requisitos: .NET SDK 10, PostgreSQL, Redis e LocalStack acessíveis. O jeito mais simples é usar os
containers do `docker-compose` da infra (Postgres em `localhost:5444`, Redis em `6379`, LocalStack em `4566`)
e criar a fila:

```powershell
aws sqs create-queue --queue-name process-donation-payment --endpoint-url http://localhost:4566 --region us-east-1
```

Então:

```powershell
cd src
dotnet restore FiapEsperancaSolidaria.Campanha.slnx
dotnet run --project FiapEsperancaSolidaria.Campanha.Api
```

- API em http://localhost:5054, documentação em http://localhost:5054/docs.
- As migrations rodam no startup e criam o schema `fundraising`.
- Com `DevBypassEnabled`, dá pra testar sem Firebase enviando `X-Dev-Role: GestorONG` ou `X-Dev-Role: Doador`.

### Docker

```powershell
docker build -f docker/dockerfile -t campanha-api:local .
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development campanha-api:local
```

---

## Rodando no Kubernetes

O deployment está em `fiap-esperanca-solidaria-infra/k8s/campaigns-api/` e usa a imagem
`projetofiap/fiap-esperanca-solidaria-campanha-api:latest` com `ASPNETCORE_ENVIRONMENT=Kubernetes`.
Service `campaigns-api` (NodePort 30081 → 8080). Para pegar uma imagem nova:

```powershell
kubectl rollout restart deployment/campaigns-deployment -n apps
```

---

## Testes

```powershell
cd src
dotnet test FiapEsperancaSolidaria.Campanha.slnx
```

---

## Servidor MCP

`FiapEsperancaSolidaria.Campanha.Mcp` é um servidor [MCP](https://modelcontextprotocol.io) via **stdio**
que permite a um assistente de IA consultar e operar a plataforma pelas APIs (faz login no `usuario-api`
e reaproveita o token).

| Ferramenta | Conta | Descrição |
|---|---|---|
| `search_campaigns`, `get_campaign`, `transparency_summary` | — | Consulta pública |
| `my_donations`, `get_donation`, `donate` | doador | Requer `DONOR_EMAIL`/`DONOR_PASSWORD` |
| `list_all_campaigns`, `create_campaign`, `cancel_campaign` | gestor | Só registradas se `MANAGER_EMAIL`/`MANAGER_PASSWORD` estiverem definidos |

Variáveis: `CAMPANHA_API_URL` (padrão `http://localhost:5054`), `USUARIO_API_URL` (padrão
`http://localhost:5043`), `DONOR_EMAIL`, `DONOR_PASSWORD`, `MANAGER_EMAIL`, `MANAGER_PASSWORD`.

```powershell
dotnet run --project src/FiapEsperancaSolidaria.Campanha.Mcp
```

---

## CI/CD

| Workflow | Gatilho | O que faz |
|---|---|---|
| `ci-push.yml` | push em `feature/**`, `bugfix/**`, `hotfix/**` | build + testes; abre PR para `develop` automaticamente se ainda não existir |
| `ci-pull-request.yml` | PR para `develop`/`main` | build + testes |
| `cd-release.yml` | tag `v*` (precisa estar no histórico de `develop`) | build, testes, scan Trivy e push de `:<tag>` e `:latest` para `projetofiap/fiap-esperanca-solidaria-campanha-api` |

Para publicar uma versão: `git tag v0.0.X origin/develop && git push origin v0.0.X`.

---

## Troubleshooting

| Sintoma | Solução |
|---|---|
| `401` em rotas protegidas | Token ausente/expirado ou `Firebase:ProjectId` diferente do emissor. |
| `403` | Token sem o papel exigido na claim `roles`. |
| Erro de CORS no navegador | Adicione a origem em `Cors:AllowedOrigins` (em k8s: `Cors__AllowedOrigins__0`). |
| Doação fica `Pending` para sempre | Fila inexistente, `SqsSettings:EmailQueueUrl` errado ou `doacao-work` parado. |
| Upload de imagem devolve URL inacessível | Ajuste `S3Settings:PublicBaseUrl` para um host que o navegador resolva. |
| Falha ao aplicar migrations no startup | Postgres inacessível ou usuário sem permissão para criar schema. |
