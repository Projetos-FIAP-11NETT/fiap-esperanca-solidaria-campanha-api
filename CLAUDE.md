# campanha-api — contexto do projeto

Ver `HACKATHON 11NETT.pdf` (enunciado original) e o histórico de decisões abaixo
para contexto completo. Este arquivo substitui/corrige o `CLAUDE-backend.md`
anterior, que ainda misturava responsabilidades de mais de um microsserviço.

## Escopo deste repositório

Só **Campanha**: CRUD (restrito a `GestorONG`), painel de transparência público
e (a implementar) recebimento da intenção de doação — publica evento, não
processa a doação. Cada responsabilidade do hackathon vive em repo próprio:

- **campanha-api** (aqui) — Campanhas + painel público + intenção de doação.
- **usuarios-api** (ainda não criado) — cadastro de doador, autenticação.
- **doacao-work** (`fiap-esperanca-solidaria-doacao-work`, hoje vazio) — consome
  o evento de doação, atualiza `ValorTotalArrecadado`.

Sem tabela de Doador/Doação neste repo **como fluxo de produto** — mas o
modelo de dados já tem `Campaign` como aggregate root de `Donation` (FK real
no schema, ver "Agregado Campaign → Donation" em Decisões fechadas). Isso foi
deixado de propósito em 2026-08-26: a relação estrutural (Campaign possui
Donations) ficou, mas a feature de criar doação (`CreateDonationCommand`,
`DonationController`) foi revertida pro estado original — **não usa** o
agregado, ainda é o protótipo solto de outra branch. Ver "Agregado" pra não
confundir as duas coisas.

## Decisões fechadas

- **Auth**: Firebase Auth. Este serviço só valida o JWT (`AuthConfig.cs`,
  `Authority = https://securetoken.google.com/<Firebase:ProjectId>`), não emite
  token nem guarda senha. RBAC via `[Authorize(Roles = "GestorONG")]` /
  `"Doador"`.
- **Mensageria**: SQS via LocalStack (MassTransit) — ainda não implementado
  nesta primeira leva de código. ⚠️ O enunciado pede literalmente "RabbitMQ ou
  Kafka"; a escolha por SQS precisa ser justificada no PDF de arquitetura.
- **Agregado Campaign → Donation (só o modelo, feature não usa ainda)**:
  `Campaign` é `IAggregateRoot` e tem `Campaign.Donations` (coleção
  read-only) + `Campaign.AddDonation(donorId, amount, paymentMethod)`, que
  valida a invariante `CanReceiveDonation()` (só campanha `Active` recebe
  doação). FK real no schema: `Donation.CampaignId → Campaigns.CampaignId`,
  `ON DELETE CASCADE` (`CampaignConfiguration.HasMany(c => c.Donations)`).
  ⚠️ **A feature de criar doação não usa esse agregado.**
  `CreateDonationCommandHandler`/`DonationController` continuam no estado
  original de outra branch: `IDonationRepository`/`Repository<T>` genérico
  ainda existem, `DonationController` é só um stub (`GET` → "Hello, world.",
  nenhuma rota de criar doação exposta), e os bugs já conhecidos continuam
  (repositório genérico `AddAsync` nunca chama `SaveChanges`, `RemoveRange`
  chama `UpdateRange`, `DonationResponse` não expõe propriedade nenhuma). Se
  algum dia ligar a feature no agregado, cuidado com uma pegadinha de EF Core:
  como `Donation.Id` é gerado no construtor (`Guid.NewGuid()`), o
  `DetectChanges` não reconhece a entidade nova automaticamente ao navegar a
  partir de um `Campaign` já rastreado — sem marcar o estado como `Added`
  explicitamente, o EF tenta um `UPDATE` em vez de `INSERT` e lança
  `DbUpdateConcurrencyException` (`CampaignRepository.UpdateAsync` já faz
  essa marcação pro cenário em que o agregado é usado via `Campaign`).
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
- **Status de Campanha (2026-08-26)**: além de `Active`/`Completed`/`Cancelled`,
  existe `Scheduled` (campanha criada com `StartDate` futura). `Campaign.Create`
  decide o status inicial comparando `StartDate.Date` com `DateTime.UtcNow.Date`
  — se ainda não chegou, `Scheduled`; senão, `Active` direto. As transições
  `Scheduled → Active` e `Active → Completed` são **automáticas**, via job
  (Hangfire, ver abaixo) — nunca manuais. `Cancelled` é a única transição
  manual que sobrou, e agora tem comando próprio: `CancelCampaignCommand`
  (`POST /api/v1/Campaign/{id}/cancel` — rota singular, ver "Rotas via
  `[controller]`" abaixo). `Campaign.Cancel()` não cancela cegamente: se a
  meta já foi atingida (`TotalRaised >= FinancialGoal` — `TotalRaised` é a
  fonte de verdade, mantida pelo `doacao-work` a partir do evento de doação
  aprovada; enquanto isso não roda neste ambiente, fallback pra soma das
  `Donations` locais), cancelar vira `Complete()` em vez de `Cancelled` — não
  faz sentido registrar como "cancelada" uma campanha que já bateu a meta.
  `UpdateCampaignCommand` **não aceita mais `Status`** — só edita
  título/descrição/datas/meta/imagem.
  `Campaign.ChangeStatus` (o switch genérico antigo) foi removido por ficar
  morto. Regra de fronteira: campanha fica `Active` a partir do dia do
  `StartDate` (inclusive) e só vira `Completed` no dia **seguinte** ao
  `EndDate` (fica `Active` durante todo o dia do `EndDate`).
- **Job de status (Hangfire)**: `RecurringJob` registrado em
  `Api/Configurations/Jobs/JobsConfig.cs`, roda 1x/dia (`Cron.Daily()`, **UTC
  padrão, sem ajuste de timezone** — meia-noite UTC, não meia-noite de
  Brasília) via `UpdateCampaignStatusesJob` (`Application/Jobs`), que dispara
  `UpdateCampaignStatusesCommand`. A consulta de quem precisa atualizar é
  `ICampaignRepository.ListPendingStatusUpdateAsync` (sem `AsNoTracking`,
  porque o handler muda o Status das entidades retornadas e salva em
  seguida). Escolhido Hangfire em vez de Quartz.NET pela API mais simples
  (`RecurringJob.AddOrUpdate` em vez de `IJob`/`ITrigger`/registro manual) e
  pelo dashboard (`/hangfire`, sem auth configurada ainda — decidir antes de
  produção de verdade). Guarda estado/histórico no mesmo Postgres já
  existente (`Hangfire.PostgreSql`), schema próprio, zero infra nova (nenhum
  container a mais). Validado rodando a API de verdade (não só compilando):
  `Bus started`, Hangfire instala os objetos SQL sozinho, dashboard responde
  200, e o recurring job fica salvo em `hangfire.hash` com `Cron: 0 0 * * *`,
  `TimeZoneId: UTC`.
- **MassTransit rebaixado pra 8.5.3 (2026-09-02)**: estava em 9.2.1
  (`FiapEsperancaSolidaria.Campanha.Queue.csproj`), que exige licença
  comercial (`MT_LICENSE`/`MT_LICENSE_PATH`) — sem isso a app inteira
  derrubava na inicialização (o healthcheck do MassTransit força a criação
  do bus assim que sobe). 8.5.x é a última major sem essa exigência. As APIs
  usadas aqui (`AddMassTransit`, `UsingAmazonSqs`,
  `KebabCaseEndpointNameFormatter`, `UseMessageRetry`) não mudaram entre as
  duas versões — rebaixar não pediu nenhuma mudança de código, só
  `MassTransit`/`MassTransit.AmazonSQS` no `.csproj`. Se algum dia quiser
  voltar pra v9+, vai precisar resolver a licença primeiro.
- **Rotas via `[controller]` (não string explícita)**: `CampaignController`
  e `DonationController` usam `[Route("api/v1/[controller]")]` — resolve pro
  nome da classe sem o sufixo `Controller`, **singular** (`/api/v1/Campaign`,
  `/api/v1/Donation`, não `/campaigns`/`/donations`). É o padrão que o time
  adotou nos dois controllers; não "corrigir" pra rota plural explícita de
  novo — já rolou mais de uma vez de alguém (inclusive eu) achar que era
  regressão e reverter sem querer.
- **Servidor MCP (`FiapEsperancaSolidaria.Campanha.Mcp`)**: projeto separado
  (só fala HTTP com campanha-api e usuario-api, não referencia o código
  interno), SDK oficial `ModelContextProtocol`, transporte **stdio** — roda
  local pelo Claude Desktop/Claude Code, então **não gasta token de API**
  (quem paga o modelo é a assinatura do cliente). Tools públicas:
  `search_campaigns`, `get_campaign`, `transparency_summary`. Tools do doador
  (agem como a conta em `DONOR_EMAIL`/`DONOR_PASSWORD`, que fazem login em
  `POST /api/v1/User/Login` da usuario-api; token em memória, renovado ao
  expirar ou ao tomar 401): `my_donations` (o recibo `GET /Donation/me`),
  `get_donation` e `donate`. `donate` é a única com efeito real e tem trava
  dupla: com `confirm=false` só devolve a prévia, e a descrição manda o modelo
  pedir confirmação explícita do usuário antes de chamar com `confirm=true`.
  O `DonorId` nunca é enviado (a campanha-api pega do token). Tools do gestor
  (`list_all_campaigns`, `create_campaign`, `cancel_campaign`; agem como a
  conta `MANAGER_EMAIL`/`MANAGER_PASSWORD`, que precisa ter o perfil
  GestorONG): **só são registradas se `MANAGER_EMAIL` estiver definido** —
  uma conexão só de doador nem enxerga ferramenta de admin. `create_campaign`
  e `cancel_campaign` têm a mesma trava de `confirm`; a prévia do cancelamento
  já avisa a regra de negócio (meta atingida => vira `Completed`, não
  `Cancelled`) e a do criar mostra o status esperado (`Active` se o início é
  hoje ou passado, `Scheduled` se futuro). Variáveis:
  `CAMPANHA_API_URL` (padrão `http://localhost:5054`), `USUARIO_API_URL`
  (padrão `http://localhost:5043`). A senha fica no ambiente do processo MCP,
  nunca no chat (passaria pelo histórico da conversa). Stdout é o canal do
  protocolo: logs vão pra stderr, nunca `Console.WriteLine`. Pra conectar:
  `dotnet build src/FiapEsperancaSolidaria.Campanha.Mcp -c Release` e apontar
  o cliente pra `dotnet <caminho>/bin/Release/net10.0/FiapEsperancaSolidaria.Campanha.Mcp.dll`
  — Claude Code: `claude mcp add campanha -e CAMPANHA_API_URL=... -e DONOR_EMAIL=... -e DONOR_PASSWORD=... -- dotnet <dll>`;
  Claude Desktop: bloco `mcpServers` no `claude_desktop_config.json` com
  `command: "dotnet"`, `args: ["<dll>"]` e o `env`. Testado de verdade via
  stdio contra a campanha-api rodando (fluxo completo doador + gestor: criar
  Active/Scheduled, título duplicado, doar, recibo com campanha cancelada,
  cancelar, tools de admin escondidas sem `MANAGER_EMAIL`), usando um login
  falso + um proxy que traduz `Bearer dev-<Papel>` no header `X-Dev-Role` do
  bypass de dev. O login real na usuario-api (Firebase) **não** foi
  exercitado — só coberto por testes com HTTP falso.

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

Endpoint de doação e mensageria (SQS) ainda **não implementados** conforme
esse repositório deveria: existe uma implementação de `Donation` vinda de
outra branch que persiste a doação localmente (contradiz a decisão acima de
só publicar evento) — pendente de decisão do time, não mexida por ora. O
modelo de dados já tem `Campaign` como aggregate root de `Donation` (FK
testada contra Postgres real), mas isso ainda não está ligado à feature de
doação — ver "Agregado Campaign → Donation" em Decisões fechadas.

CI/CD ainda não criado. Manifests de Kubernetes existem só para a infra
compartilhada (Postgres, Redis, Elasticsearch, LocalStack etc., no repo
`fiap-esperanca-solidaria-infra`) — falta o manifest de deployment do próprio
`campanha-api`.

Status automático de campanha (`Scheduled`/`Active`/`Completed` via job
Hangfire diário) e `CancelCampaignCommand` implementados e testados —
`dotnet test` (unitários da entidade/handlers + integração via
testcontainers) **e** rodando a API de verdade via `dotnet run` (`Bus
started`, dashboard do Hangfire em `/hangfire` respondendo, recurring job
persistido com o cron certo).
