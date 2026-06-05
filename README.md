> **Nota:** Esta e a copia independente do microsservico **CatalogAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-catalog-api . (da raiz deste repo).

---

# CatalogAPI â€” FCG MicrosserviÃ§o de CatÃ¡logo

CatÃ¡logo de jogos, promoÃ§Ãµes e biblioteca do usuÃ¡rio. **Inicia o fluxo de compra** publicando `OrderPlacedEvent` e **consome** `PaymentProcessedEvent` para materializar a biblioteca apÃ³s aprovaÃ§Ã£o do pagamento.

---

## Responsabilidades

- CRUD de `Jogo` (admin)
- CRUD de `Promocao` (admin) â€” vinculada a um Jogo, com tipo de desconto e validade
- CÃ¡lculo do preÃ§o final (Jogo Ã— PromoÃ§Ã£o vigente) â€” `PricingService`
- Iniciar compra: publica `OrderPlacedEvent` e retorna `202 Accepted` com `orderId`
- Materializar `BibliotecaJogo` ao consumir `PaymentProcessedEvent(Approved)`
- Listar/consultar biblioteca do usuÃ¡rio logado

## NÃ£o Ã© responsabilidade

- Persistir pagamentos (isso Ã© PaymentsAPI)
- Notificar usuÃ¡rios (isso Ã© NotificationsAPI)

---

## Endpoints

| MÃ©todo | Rota | Auth | DescriÃ§Ã£o |
|---|---|---|---|
| GET | `/api/jogos` | pÃºblico | Lista jogos ativos |
| GET | `/api/jogos/{id}` | pÃºblico | Detalhes de um jogo |
| POST | `/api/jogos` | Admin | Cria jogo |
| PUT | `/api/jogos/{id}` | Admin | Atualiza jogo |
| DELETE | `/api/jogos/{id}` | Admin | Soft delete |
| GET | `/api/promocoes` | pÃºblico | Lista promoÃ§Ãµes |
| GET | `/api/promocoes/{id}` | pÃºblico | Detalha promoÃ§Ã£o |
| POST | `/api/promocoes` | Admin | Cria promoÃ§Ã£o |
| PUT | `/api/promocoes/{id}` | Admin | Atualiza |
| DELETE | `/api/promocoes/{id}` | Admin | Soft delete |
| GET | `/api/biblioteca` | autenticado | Biblioteca do usuÃ¡rio logado |
| GET | `/api/biblioteca/{id}` | autenticado | Item da biblioteca |
| **POST** | **`/api/biblioteca`** | **autenticado** | **Inicia compra â†’ 202 Accepted `{ orderId, status: "pending" }`** |
| GET | `/health` | pÃºblico | Health check |

## Eventos publicados

| Evento | Quando | Payload |
|---|---|---|
| `OrderPlacedEvent` | ApÃ³s `POST /api/biblioteca` validar Jogo/Promocao e calcular preÃ§o | `MessageId, OccurredAt, OrderId, UserId, UserEmail, UserName, GameId, GameName, OriginalPrice, FinalPrice, PromocaoId?` |

## Eventos consumidos

| Evento | Origem | AÃ§Ã£o |
|---|---|---|
| `PaymentProcessedEvent` (Approved) | PaymentsAPI | Insere `BibliotecaJogo` + `ProcessedMessage` em uma transaÃ§Ã£o |
| `PaymentProcessedEvent` (Rejected) | PaymentsAPI | Apenas registra `ProcessedMessage` (nÃ£o toca biblioteca) |

### IdempotÃªncia (2 camadas)

1. **`processed_messages`** â€” `INSERT` no inÃ­cio do handler; duplicatas detectadas via PK `MessageId`.
2. **Ãndice Ãºnico parcial** em `biblioteca_jogos (usuario_id, jogo_id) WHERE is_active = TRUE` â€” Ãºltima linha de defesa, mesmo se o redelivery contornar a camada 1.

---

## VariÃ¡veis de ambiente

| VariÃ¡vel | Default | DescriÃ§Ã£o |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=catalog_db;...` | Postgres do serviÃ§o |
| `JwtSettings__Secret` | dev placeholder | Mesma chave usada pelo UsersAPI |
| `JwtSettings__Issuer` / `__Audience` | `FCG.UsersAPI` / `FCG.Services` | Mesma config dos demais |
| `RabbitMq__Host`/`Port`/`Username`/`Password` | localhost/5672/guest/guest | Broker |
| `Swagger__Enabled` | `true` | UI Swagger |

CatalogAPI **nÃ£o** emite tokens â€” apenas valida. NÃ£o registra `ITokenService`.

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

# 2) Cadastrar jogo (precisa ser admin â€” direto no banco aumenta a Role do user)
$h = @{ Authorization = "Bearer $token" }
$jogo = Invoke-RestMethod -Uri http://localhost:5002/api/jogos -Method Post -Headers $h -Body '{"nome":"Doom","descricao":"FPS clÃ¡ssico","preco":99.90}' -ContentType 'application/json'

# 3) Iniciar compra â†’ 202 Accepted
Invoke-RestMethod -Uri http://localhost:5002/api/biblioteca -Method Post -Headers $h -Body "{`"jogoId`":`"$($jogo.id)`"}" -ContentType 'application/json'
```

---

## DecisÃµes arquiteturais

- **InversÃ£o do fluxo de compra (vs. monolito):** `POST /api/biblioteca` nÃ£o escreve mais a biblioteca direto â€” apenas publica `OrderPlacedEvent`. A biblioteca sÃ³ Ã© materializada quando o consumer de `PaymentProcessedEvent(Approved)` executa. Isso modela corretamente a transaÃ§Ã£o assÃ­ncrona.
- **`OrderId` gerado pelo CatalogAPI:** correlaciona o evento de compra com o resultado do pagamento sem roundtrip sÃ­ncrono.
- **`PricingService` isolado:** lÃ³gica de cÃ¡lculo de desconto extraÃ­da para classe testÃ¡vel (singleton, sem estado).
- **`Promocao` vive aqui** (nÃ£o em PaymentsAPI): desconto Ã© concern de catÃ¡logo/pricing, e precisa estar disponÃ­vel no momento da validaÃ§Ã£o prÃ©-compra.
- **EF Core `EnsureCreated()`:** simplifica setup em demo. ProduÃ§Ã£o: migrations versionadas.

