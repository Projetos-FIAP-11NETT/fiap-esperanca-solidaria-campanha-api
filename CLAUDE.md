# campanha-api — contexto do projeto

Ver `HACKATHON 11NETT.pdf` (enunciado original) e o histórico de decisões abaixo
para contexto completo. Este arquivo substitui/corrige o `CLAUDE-backend.md`
anterior, que ainda misturava responsabilidades de mais de um microsserviço.

## Escopo deste repositório

**Campanha** (CRUD restrito a `GestorONG`, painel de transparência público) e
**Doação** — decisão revista em 2026-08-26: ao contrário do que este arquivo
dizia antes, o registro de doação **é persistido diretamente aqui**, como
parte do agregado `Campaign` (`Campaign` é a raiz do agregado; `Donation` é
entidade filha, sem repositório próprio — só existe através de
`Campaign.AddDonation(...)`, FK `Donation.CampaignId → Campaigns.CampaignId`,
`ON DELETE CASCADE`). Não publica mais evento de doação como intenção original;
o fluxo é síncrono via `POST /api/v1/donations`.

- **campanha-api** (aqui) — Campanhas + painel público + registro de doação.
- **usuarios-api** (ainda não criado) — cadastro de doador, autenticação.
- **doacao-work** (`fiap-esperanca-solidaria-doacao-work`, hoje vazio) — papel
  em aberto agora que a doação é persistida aqui (ver "Decisões em aberto").

A identidade do doador ainda vem só do claim do JWT (Firebase) — mas
`CreateDonationCommand.DonorId` hoje é campo do request, não lido do claim
ainda (gap conhecido, não resolvido nesta mudança).

## Decisões fechadas

- **Auth**: Firebase Auth. Este serviço só valida o JWT (`AuthConfig.cs`,
  `Authority = https://securetoken.google.com/<Firebase:ProjectId>`), não emite
  token nem guarda senha. RBAC via `[Authorize(Roles = "GestorONG")]` /
  `"Doador"`.
- **Mensageria**: SQS via LocalStack (MassTransit) — ainda não implementado
  nesta primeira leva de código. ⚠️ O enunciado pede literalmente "RabbitMQ ou
  Kafka"; a escolha por SQS precisa ser justificada no PDF de arquitetura.
  ⚠️ Com a doação agora persistida direto aqui (ver "Escopo deste
  repositório"), o motivo original de existir mensageria (publicar evento de
  intenção de doação pro doacao-work consumir) não se aplica mais do jeito que
  foi pensado — precisa decidir se ainda existe uso pra SQS/MassTransit aqui
  (ex.: publicar evento de doação *aprovada* pra notificação) ou se isso migra
  todo pro doacao-work/notificacao-lambda.
- **Agregado Campaign → Donation**: `Campaign` é `IAggregateRoot`; `Donation`
  não tem repositório próprio nem é acessível fora do agregado — é sempre
  criada via `Campaign.AddDonation(donorId, amount, paymentMethod)`, que
  valida a invariante `CanReceiveDonation()` (só campanha `Active` recebe
  doação; senão `BusinessException`, mapeada pro middleware como 422).
  Persistência via `ICampaignRepository.UpdateAsync` — **não** existe mais
  `IDonationRepository`/`Repository<T>` genérico (removidos: só faziam
  sentido com `Donation` como aggregate root próprio). ⚠️ Pegadinha de EF
  Core: como `Donation.Id` é gerado no construtor (`Guid.NewGuid()`), o
  `DetectChanges` não reconhece a entidade nova automaticamente ao navegar a
  partir de um `Campaign` já rastreado — sem marcar o estado como `Added`
  explicitamente (feito em `CampaignRepository.UpdateAsync`), o EF tenta um
  `UPDATE` em vez de `INSERT` e lança `DbUpdateConcurrencyException`. Se
  adicionar outra entidade filha ao agregado com chave gerada no cliente,
  repetir esse padrão.
  `DonationStatus` começa em `Pending` — nada hoje transiciona pra `Approved`
  nem atualiza `Campaign.TotalRaised`; esse fluxo (provavelmente via
  aprovação de pagamento) ainda não foi implementado.
- **Banco**: PostgreSQL (`campanha-db`, já provisionado no repo de infra em
  `k8s/shared/postgres-campanha`), só a entidade `Campanha`. Manifests
  (`postgres-pvc.yaml`, `postgres-statefulSet.yaml`, `postgres-svc.yaml`)
  testados manualmente contra um cluster Kubernetes local (Docker Desktop) —
  sobem limpo, PVC bind normalmente na StorageClass padrão local (a
  `auto-ebs-sc` do manifest é só pra AWS EBS CSI Driver em cluster real, fica
  sem uso localmente). ⚠️ `appsettings.json` local usa `esperancasolidaria-db`
  como nome do banco, divergente do `campanha-db` provisionado — precisa
  padronizar. Falta também o manifest de deployment do próprio `campanha-api`
  (só a infra compartilhada — Postgres, Redis, Elasticsearch etc. — tem
  manifest hoje).
- **Cache**: Redis (`AddStackExchangeRedisCache`), registrado em
  `InfrastructureConfig.AddInfrastructure`. Usado hoje só para cachear
  `GET /campanhas/publicas` (`CachingBehavior` no pipeline do MediatR via
  `ICacheableQuery`), TTL curto (30s). Chave varia por filtro de busca
  (`CacheKeys.CampanhasPublicas(titulo)`).
- **Documentação da API**: OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) +
  Scalar em `/docs` — trocado do Swashbuckle/Swagger UI original.
  `Api/Configurations/OpenApi/OpenApiConfiguration.cs` registra o documento
  (incluindo o security scheme Bearer, já que a API nativa não gera isso
  sozinha como o Swashbuckle fazia) e `OpenApiPipeline.cs` mapeia
  `/openapi/v1.json` e `/docs`.

## Decisões em aberto

- **Observabilidade**: ainda não fechada (Zabbix+Grafana literal vs.
  Prometheus+Grafana+Loki). Só existe `/health` (ASP.NET health checks) por
  enquanto — neutro em relação a essa escolha.

## Convenções de código (herdadas dos repos de referência FiapCloudGames)

- Clean-ish layering em projetos separados: `Api / Application / Domain /
  Infrastructure / Observability / Tests`, `.slnx`, `net10.0`.
- CQRS vertical-slice com MediatR: `Application/Features/{Entidade}Feature/
  Commands|Queries/{Verbo}{Entidade}/{Command,Handler,Validator}.cs`.
- FluentValidation **de fato registrado** via `ValidationBehavior` no pipeline
  do MediatR (bug conhecido no catalog-api de referência: validadores existiam
  mas nunca eram chamados — corrigido aqui).
- Serilog **de fato inicializado** via `UseSerilog` (outro gap do catalog-api
  de referência).
- Configuração de infra (DbContext, cache, repositórios) centralizada em
  `Infrastructure/Configurations/InfrastructureConfig.AddInfrastructure(configuration)`
  — inclui o `AddDbContext<AppDbContext>`, não fica solto no `Program.cs` da
  Api.
- Healthchecks isolados em `Api/Configurations/HealthCheckConfig.cs`
  (`AddHealthCheckConfiguration` + `MapHealthCheckEndpoints`), não inline no
  `Program.cs`. Expõe `/health`, `/health/ready`, `/health/live`.

## Pegadinha de namespace (já resolvida, não reintroduzir)

O namespace raiz do projeto é `FiapEsperancaSolidaria.Campanha.*` e a entidade
principal também se chama `Campanha` — uma referência **não-qualificada** ou
parcialmente qualificada (`Campanha`, ou `Domain.Entities.Campanha` de dentro
de um namespace que também tenha um segmento chamado `Domain`) pode resolver
para o namespace em vez da classe (erro `CS0118`), ou resolver para o
segmento de namespace errado. Ao referenciar a entidade fora do projeto
`Domain`, use sempre um alias explícito:

```csharp
using CampanhaEntity = FiapEsperancaSolidaria.Campanha.Domain.Entities.Campanha;
```

Evite também criar pastas/namespaces chamados `Domain` fora do projeto Domain
(ex.: dentro de `Tests`) — mesmo problema.

## Status atual

Estrutura base criada e validada (`dotnet build`, `dotnet test`, `docker build`
todos passando): Domain/Application/Infrastructure/Observability/Api/Tests com
CRUD de Campanha completo, migrations do EF Core (título único + status como
int), busca por título em `GET /campanhas/publicas` (com escape de coringas
do `ILIKE`), cache Redis e testes de integração. Migrations testadas de
verdade contra Postgres real (local via Docker e via manifests de Kubernetes
num cluster local) — sobem limpo do zero.

`POST /api/v1/donations` implementado e testado de ponta a ponta contra
Postgres real: `Donation` como entidade filha do agregado `Campaign` (FK real,
`ON DELETE CASCADE`), validado tanto o caminho feliz quanto as invariantes
(campanha inexistente → 404, campanha não-`Active` → 422). Mensageria (SQS)
segue não implementada — ver "Decisões fechadas" pro estado em aberto sobre
se ainda faz sentido aqui.

CI/CD ainda não criado. Manifests de Kubernetes existem só para a infra
compartilhada (Postgres, Redis, Elasticsearch, LocalStack etc., no repo
`fiap-esperanca-solidaria-infra`) — falta o manifest de deployment do próprio
`campanha-api`.
