# fiap-esperanca-solidaria-campanha-api

API de Campanhas do MVP "Conexão Solidária" (ONG Esperança Solidária) — Hackathon
FIAP Pós Tech. Responsável por: gestão de campanhas (CRUD, restrito a `GestorONG`),
painel de transparência público e recebimento da intenção de doação (publica um
evento de mensageria, não processa a doação).

> Doação (Worker) e Cadastro/Autenticação de usuários vivem em repositórios
> separados (`fiap-esperanca-solidaria-doacao-work` e um futuro `usuarios-api`).
> Este repositório só valida o JWT emitido pelo Firebase Auth — não emite token
> nem armazena senha.

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API (Controllers) |
| Arquitetura | Clean-ish (Api / Application / Domain / Infrastructure / Observability), CQRS com MediatR |
| Validação | FluentValidation (via `ValidationBehavior` no pipeline do MediatR) |
| Banco de dados | PostgreSQL (EF Core) |
| Autenticação | Firebase Auth — API só valida o JWT (`Authorize(Roles = "GestorONG")` / `"Doador"`) |
| Logs | Serilog (console, estruturado, correlação por `X-Correlation-Id`) |
| Testes | xUnit + Moq + FluentAssertions |
| Container | Docker (build multi-stage) |

## Estrutura

```
src/
  FiapEsperancaSolidaria.Campanha.slnx
  FiapEsperancaSolidaria.Campanha.Api             (Controllers, Program.cs, Configurations)
  FiapEsperancaSolidaria.Campanha.Application     (MediatR: Features/CampanhaFeature/Commands|Queries)
  FiapEsperancaSolidaria.Campanha.Domain          (Entities, Enums, Exceptions, Contracts)
  FiapEsperancaSolidaria.Campanha.Infrastructure  (EF Core/Postgres, Repositories, Migrations)
  FiapEsperancaSolidaria.Campanha.Observability   (correlation id, exception middleware)
  FiapEsperancaSolidaria.Campanha.Tests           (xUnit)
docker/Dockerfile
```

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/v1/campanhas/publicas` | Público | Painel de transparência — só campanhas `Ativa` |
| GET | `/api/v1/campanhas/{id}` | Público | Detalhe de uma campanha |
| POST | `/api/v1/campanhas` | `GestorONG` | Cria campanha |
| PUT | `/api/v1/campanhas/{id}` | `GestorONG` | Edita campanha |
| GET | `/health` | Público | Health check (Postgres) |

> Endpoint de doação (`POST /api/v1/doacoes`, publica evento na fila) ainda não
> implementado nesta primeira leva — ver seção "Próximos passos".

Swagger/OpenAPI disponível em `/swagger` em ambiente de desenvolvimento.

## Rodando localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL acessível (local, Docker, ou via `docker-compose` do repo
  `fiap-esperanca-solidaria-infra` — sobe na porta `5444`)
- Um projeto Firebase (para validar o `ProjectId` usado na autenticação)

### 1. Configurar

Copie `src/FiapEsperancaSolidaria.Campanha.Api/appsettings.Example.json` para
`appsettings.Development.json` (mesmo diretório) e preencha:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5444;Database=campanha-db;Username=postgres;Password=postgres;"
  },
  "Firebase": {
    "ProjectId": "<id-do-projeto-firebase>"
  }
}
```

### 2. Subir o Postgres

Usando o `docker-compose.yaml` do repo `fiap-esperanca-solidaria-infra` (recomendado,
já provisiona o banco `campanha-db` compartilhado), ou qualquer Postgres local na
porta usada acima.

### 3. Rodar a API

```bash
cd src/FiapEsperancaSolidaria.Campanha.Api
dotnet run
```

A aplicação aplica as migrações do EF Core automaticamente na inicialização
(`app.MigrateDatabase()` em `Program.cs`).

### 4. Rodar via Docker

```bash
docker build -f docker/Dockerfile -t esperanca-solidaria/campanha-api:latest .
docker run -d -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5444;Database=campanha-db;Username=postgres;Password=postgres;" \
  -e Firebase__ProjectId="<id-do-projeto-firebase>" \
  --name campanha-api esperanca-solidaria/campanha-api:latest
```

### Testes

```bash
dotnet test src/FiapEsperancaSolidaria.Campanha.Tests
```

### Gerando novas migrações

```bash
dotnet tool install --global dotnet-ef   # se ainda não tiver
cd src
dotnet ef migrations add NomeDaMigracao \
  --project FiapEsperancaSolidaria.Campanha.Infrastructure \
  --startup-project FiapEsperancaSolidaria.Campanha.Api \
  --output-dir Data/Migrations
```

## Próximos passos

- Endpoint `POST /api/v1/doacoes` publicando `DoacaoRecebidaEvent` via MassTransit
  + AWS SQS (LocalStack).
- Cache Redis no painel de transparência público.
- Upload de imagem da campanha via URL pré-assinada do S3 (LocalStack).
- `/metrics` e dashboard de observabilidade (stack ainda em definição).
- Pipeline de CI/CD (GitHub Actions) e manifests de Kubernetes.
