> **Nota:** Esta e a copia independente do microsservico **CatalogAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-catalog-api . (da raiz deste repo).

---

# CatalogAPI — FCG Microsserviço de Catálogo

Catálogo de jogos, promoções e biblioteca do usuário. **Inicia o fluxo de compra** publicando `OrderPlacedEvent` e **consome** `PaymentProcessedEvent` para materializar a biblioteca após aprovação do pagamento.

---

## Responsabilidades

- CRUD de `Jogo` (admin)
- CRUD de `Promocao` (admin) — vinculada a um Jogo, com tipo de desconto e validade
- Cálculo do preço final (Jogo × Promoção vigente) — `PricingService`
- Iniciar compra: publica `OrderPlacedEvent` e retorna `202 Accepted` com `orderId`
- Materializar `BibliotecaJogo` ao consumir `PaymentProcessedEvent(Approved)`
- Listar/consultar biblioteca do usuário logado

## Não é responsabilidade

- Persistir pagamentos (isso é PaymentsAPI)
- Notificar usuários (isso é NotificationsAPI)

---

## Endpoints

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/api/jogos` | público | Lista jogos ativos |
| GET | `/api/jogos/search?q=` | público | Busca avançada (Elasticsearch): fuzzy search + ordenação por relevância |
| GET | `/api/jogos/{id}` | público | Detalhes de um jogo |
| POST | `/api/jogos` | Admin | Cria jogo |
| PUT | `/api/jogos/{id}` | Admin | Atualiza jogo |
| DELETE | `/api/jogos/{id}` | Admin | Soft delete |
| GET | `/api/promocoes` | público | Lista promoções |
| GET | `/api/promocoes/{id}` | público | Detalha promoção |
| POST | `/api/promocoes` | Admin | Cria promoção |
| PUT | `/api/promocoes/{id}` | Admin | Atualiza |
| DELETE | `/api/promocoes/{id}` | Admin | Soft delete |
| GET | `/api/biblioteca` | autenticado | Biblioteca do usuário logado |
| GET | `/api/biblioteca/{id}` | autenticado | Item da biblioteca |
| **POST** | **`/api/biblioteca`** | **autenticado** | **Inicia compra → 202 Accepted `{ orderId, status: "pending" }`** |
| POST | `/api/jogos/{id}/avaliacoes` | autenticado | Avalia um jogo (nota de 1 a 5 e comentário) |
| GET | `/api/jogos/{id}/avaliacoes` | público | Lista as avaliações, das mais recentes para as mais antigas |
| GET | `/api/jogos/{id}/avaliacoes/resumo` | público | Nota média e total de avaliações |
| GET | `/health` | público | Health check |
| GET | `/metrics` | interno | Métricas no formato Prometheus |

---

## Persistência poliglota (Fase 3)

Este serviço usa quatro armazenamentos, cada um para o que faz melhor:

| Onde | O que guarda | Por quê |
|---|---|---|
| **PostgreSQL** | Jogos, promoções, biblioteca do usuário | Dados que exigem consistência rigorosa e transação |
| **MongoDB** | Avaliações de jogos | Documento naturalmente flexível (comentário em texto livre), volume que cresce sem limite e sem necessidade de transação |
| **Redis** | Cópia temporária do catálogo e das promoções em vigor | Consultas muito repetidas, que devolvem sempre o mesmo resultado entre uma alteração e outra |
| **Elasticsearch** | Índice de busca dos jogos (nome/descrição) | Motor de busca full-text, independente do modelo relacional, com fuzzy search e ranking por relevância |

### Avaliações (MongoDB)

Regras: só avalia quem possui o jogo na biblioteca (`403`) e apenas uma vez por jogo (`409`); nota fora de 1 a 5 é recusada (`400`). O resumo com média e total é calculado pelo próprio banco, e a coleção tem índice por jogo e data.

### Cache (Redis)

Padrão *cache-aside*: a primeira consulta vai ao banco e guarda o resultado; as seguintes vêm do cache. **A cópia é descartada assim que o dado muda** — criar, alterar ou remover um jogo ou promoção invalida o cache, evitando exibir informação desatualizada. Há também um prazo de validade de 5 minutos como segurança adicional.

Uma falha do Redis não derruba o serviço: o acesso ao cache é tolerante a erro e a consulta segue para o banco. Na validação, a mesma consulta caiu de **584 ms para 17 ms** entre a primeira e a segunda chamada.

### Busca (Elasticsearch)

`GET /api/jogos/search?q=&page=&pageSize=` é um motor de busca independente da consulta tradicional ao Postgres. Internamente é um `multi_match` nos campos `nome` (peso 2x) e `descricao`, com:

- **Fuzzy search**: `Fuzziness.Auto` tolera erros de digitação (ex. `q=corrda` ainda encontra "Corrida Extrema X").
- **Ordenação por relevância**: resultados vêm ordenados por `_score` (relevância do Elasticsearch), devolvido no campo `score` de cada item.

**Sincronização**: o índice não é um processo batch separado — o próprio `JogoService` indexa/reindexa o jogo a cada `POST`/`PUT` e remove do índice a cada `DELETE` (soft delete), na mesma unidade de trabalho da escrita no Postgres. Assim como o Redis, a chamada ao Elasticsearch é tolerante a erro: se o índice estiver indisponível, o CRUD do catálogo continua funcionando normalmente e só a busca fica temporariamente afetada (`503`). No startup, o serviço garante a existência do índice e reindexa todos os jogos ativos do Postgres, autocorrigindo qualquer divergência.

**Testando a busca:**

```powershell
# Busca simples (navegador ou PowerShell)
Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/search?q=witcher"

# Fuzzy search — erro de digitação de propósito, ainda encontra o jogo
Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/search?q=withcer"

# Busca por palavra da descricao, ordenada por relevancia (campo "score")
Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/search?q=mundo%20aberto"
```

Ou pelo Swagger (http://localhost:5002/swagger), em `GET /api/jogos/search` → **Try it out**.

Para validar a sincronização (criar → buscar → editar → buscar → excluir → buscar):

```powershell
$login = Invoke-RestMethod -Uri http://localhost:5001/api/auth/login -Method Post `
  -Body '{"email":"admin@fcg.com","password":"Admin@1234"}' -ContentType 'application/json'
$h = @{ Authorization = "Bearer $($login.token)" }

$jogo = Invoke-RestMethod -Uri http://localhost:5002/api/jogos -Method Post -Headers $h `
  -Body '{"nome":"Dragonborn Saga","descricao":"RPG de fantasia com dragoes e magia","preco":59.90}' `
  -ContentType 'application/json'
$id = $jogo.id

Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/search?q=dragonbron"   # ja aparece, com typo

Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/$id" -Method Put -Headers $h `
  -Body '{"nome":"Dragonborn Saga Remastered","descricao":"Edicao remasterizada","preco":69.90,"ativo":true}' `
  -ContentType 'application/json'
Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/search?q=remastered"   # acha pelo nome novo

Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/$id" -Method Delete -Headers $h
Invoke-RestMethod -Uri "http://localhost:5002/api/jogos/search?q=dragonborn"   # volta vazio
```

Se `admin@fcg.com`/`Admin@1234` não existir na base (dados de testes anteriores podem ter sobrescrito o seed), registre um usuário via `POST /api/auth/register` e promova a Admin direto no banco:

```powershell
docker exec -it fcg-postgres psql -U postgres -d users_db -c "UPDATE usuarios SET role = 'Admin' WHERE email = 'seu-email@teste.com';"
```

Para inspecionar o índice direto no Elasticsearch, sem passar pela API:

```powershell
Invoke-RestMethod -Uri "http://localhost:9200/catalog-jogos/_search?size=20" | ConvertTo-Json -Depth 5
```

### Observabilidade (Fase 3)

O serviço publica métricas em `/metrics`, coletadas pelo Prometheus e exibidas no painel do Grafana: tempo de resposta, volume de requisições por rota e por código HTTP, e taxa de erros. O endereço é interno ao cluster e não é exposto pelo gateway.

---

## Eventos publicados

| Evento | Quando | Payload |
|---|---|---|
| `OrderPlacedEvent` | Após `POST /api/biblioteca` validar Jogo/Promocao e calcular preço | `MessageId, OccurredAt, OrderId, UserId, UserEmail, UserName, GameId, GameName, OriginalPrice, FinalPrice, PromocaoId?` |

## Eventos consumidos

| Evento | Origem | Ação |
|---|---|---|
| `PaymentProcessedEvent` (Approved) | PaymentsAPI | Insere `BibliotecaJogo` + `ProcessedMessage` em uma transação |
| `PaymentProcessedEvent` (Rejected) | PaymentsAPI | Apenas registra `ProcessedMessage` (não toca biblioteca) |

### Idempotência (2 camadas)

1. **`processed_messages`** — `INSERT` no início do handler; duplicatas detectadas via PK `MessageId`.
2. **Índice único parcial** em `biblioteca_jogos (usuario_id, jogo_id) WHERE is_active = TRUE` — última linha de defesa, mesmo se o redelivery contornar a camada 1.

---

## Variáveis de ambiente

| Variável | Default | Descrição |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=catalog_db;...` | Postgres do serviço |
| `JwtSettings__Secret` | dev placeholder | Mesma chave usada pelo UsersAPI |
| `JwtSettings__Issuer` / `__Audience` | `FCG.UsersAPI` / `FCG.Services` | Mesma config dos demais |
| `RabbitMq__Host`/`Port`/`Username`/`Password` | localhost/5672/guest/guest | Broker |
| `MongoDb__ConnectionString` | `mongodb://...@mongodb:27017` | MongoDB das avaliações |
| `MongoDb__Database` | `catalog` | Nome do banco no MongoDB |
| `Redis__ConnectionString` | `redis:6379` | Cache distribuído |
| `Elasticsearch__Uri` | `http://localhost:9200` | Endpoint do Elasticsearch/OpenSearch (gerenciado ou self-hosted) |
| `Elasticsearch__IndexName` | `catalog-jogos` | Índice usado pela busca |
| `Elasticsearch__ApiKey` | vazio | API key, apenas quando o cluster exige autenticação (Elastic Cloud/OpenSearch gerenciado) |
| `Swagger__Enabled` | `true` | UI Swagger |

CatalogAPI **não** emite tokens — apenas valida. Não registra `ITokenService`.

---

## Rodando localmente

```powershell
docker compose -f ..\Orchestration\docker-compose.yml up -d postgres rabbitmq
dotnet run --project src/FCG.Catalog.Api
```

Para testar o fluxo de compra:

```powershell
# 1) Obter um token no UsersAPI primeiro
$body = '{"email":"anderson@koester.com.br","password":"Senha@1234"}'
$token = (Invoke-RestMethod -Uri http://localhost:5001/api/auth/login -Method Post -Body $body -ContentType 'application/json').token

# 2) Cadastrar jogo (precisa ser admin — direto no banco aumenta a Role do user)
$h = @{ Authorization = "Bearer $token" }
$jogo = Invoke-RestMethod -Uri http://localhost:5002/api/jogos -Method Post -Headers $h -Body '{"nome":"Doom","descricao":"FPS clássico","preco":99.90}' -ContentType 'application/json'

# 3) Iniciar compra → 202 Accepted
Invoke-RestMethod -Uri http://localhost:5002/api/biblioteca -Method Post -Headers $h -Body "{`"jogoId`":`"$($jogo.id)`"}" -ContentType 'application/json'
```

---

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`):

- **CI** — em todo push ou pull request: `dotnet restore` + `build` do `CatalogAPI.sln` + `dotnet test` do projeto `tests/FCG.Catalog.Api.Tests` (xUnit + Moq, mesmo padrão de `users-api`/`payments-api`).
- **CD** — só em push na `master`: build e publicação da imagem Docker no **Azure Container Registry** (`$ACR_LOGIN_SERVER/catalog-api`, tags `latest` e `<sha>`), autenticando com o Service Principal do projeto.
- **Deploy** — na sequência, aplica `k8s/configmap.yaml` e `k8s/secret.yaml` (segredos injetados via `envsubst` a partir de GitHub Secrets — nenhum valor real fica no repositório) e `k8s/deployment.yaml`/`service.yaml` com a imagem recém-publicada no **AKS**, confirmando rolling update sem downtime com `kubectl rollout status`.

Secrets do GitHub usados por este workflow (nível de organização `FIAP-Grupo9`): `AZURE_CREDENTIALS`, `ACR_LOGIN_SERVER`, `AKS_RESOURCE_GROUP`, `AKS_CLUSTER_NAME`, `JWT_SECRET`, `CATALOG_DB_PASSWORD`, `MONGODB_ROOT_PASSWORD`, `ELASTICSEARCH_APIKEY`. A infraestrutura compartilhada (Postgres, RabbitMQ, MongoDB, Redis, Elasticsearch, Kong) é aplicada separadamente pelo workflow `deploy-infra.yml` do repositório `orchestration` — ver o README dele.

A imagem aplicada no `Deployment` é sempre a tag `<sha>` do commit (nunca `latest`): isso torna o rollout determinístico — o mesmo `kubectl apply` nunca reaplica uma imagem já em execução por engano, e dá pra rastrear exatamente qual commit está rodando no cluster a qualquer momento.

## Decisões arquiteturais

- **Inversão do fluxo de compra (vs. monolito):** `POST /api/biblioteca` não escreve mais a biblioteca direto — apenas publica `OrderPlacedEvent`. A biblioteca só é materializada quando o consumer de `PaymentProcessedEvent(Approved)` executa. Isso modela corretamente a transação assíncrona.
- **`OrderId` gerado pelo CatalogAPI:** correlaciona o evento de compra com o resultado do pagamento sem roundtrip síncrono.
- **`PricingService` isolado:** lógica de cálculo de desconto extraída para classe testável (singleton, sem estado).
- **`Promocao` vive aqui** (não em PaymentsAPI): desconto é concern de catálogo/pricing, e precisa estar disponível no momento da validação pré-compra.
- **EF Core `EnsureCreated()`:** simplifica setup em demo. Produção: migrations versionadas.
- **Elasticsearch sincronizado pelo próprio `JogoService` (vs. job/outbox separado):** mais simples e, para este volume de escrita, suficiente. O trade-off é uma janela pequena de inconsistência entre o commit no Postgres e o índice — aceitável porque a busca não é fonte de verdade e o reindex de startup autocorrige.

