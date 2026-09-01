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
| GET | `/health` | público | Health check |

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

## Decisões arquiteturais

- **Inversão do fluxo de compra (vs. monolito):** `POST /api/biblioteca` não escreve mais a biblioteca direto — apenas publica `OrderPlacedEvent`. A biblioteca só é materializada quando o consumer de `PaymentProcessedEvent(Approved)` executa. Isso modela corretamente a transação assíncrona.
- **`OrderId` gerado pelo CatalogAPI:** correlaciona o evento de compra com o resultado do pagamento sem roundtrip síncrono.
- **`PricingService` isolado:** lógica de cálculo de desconto extraída para classe testável (singleton, sem estado).
- **`Promocao` vive aqui** (não em PaymentsAPI): desconto é concern de catálogo/pricing, e precisa estar disponível no momento da validação pré-compra.
- **EF Core `EnsureCreated()`:** simplifica setup em demo. Produção: migrations versionadas.

