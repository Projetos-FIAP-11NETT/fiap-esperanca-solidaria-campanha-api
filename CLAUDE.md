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

Sem tabela de Doador/Doação neste repo. A identidade do doador vem só do claim
do JWT (Firebase).

## Decisões fechadas

- **Auth**: Firebase Auth. Este serviço só valida o JWT (`AuthConfig.cs`,
  `Authority = https://securetoken.google.com/<Firebase:ProjectId>`), não emite
  token nem guarda senha. RBAC via `[Authorize(Roles = "GestorONG")]` /
  `"Doador"`.
- **Mensageria**: SQS via LocalStack (MassTransit) — ainda não implementado
  nesta primeira leva de código. ⚠️ O enunciado pede literalmente "RabbitMQ ou
  Kafka"; a escolha por SQS precisa ser justificada no PDF de arquitetura.
- **Banco**: PostgreSQL (`campanha-db`, já provisionado no repo de infra em
  `k8s/shared/postgres-campanha`), só a entidade `Campanha`.

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
CRUD de Campanha completo + migração inicial do EF Core. Endpoint de doação e
mensageria ainda **não implementados** (adiados a pedido do usuário nesta
sessão). CI/CD e manifests de Kubernetes também ainda não criados.
