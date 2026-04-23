# Gestão de Pedidos (Teste Técnico)

MVP de um sistema de gestão de pedidos com:

- API: .NET 8 Web API + EF Core
- DB: PostgreSQL
- Mensageria: Azure Service Bus (primário) / RabbitMQ (secundário e usado no local)
- Worker: consome eventos `OrderCreated` e atualiza o status de forma assíncrona
- Frontend: React + Vite + Tailwind (UI com polling)
- Infra: Docker + Docker Compose (api, worker, frontend, postgres, pgadmin, rabbitmq)

## Por que Azure Service Bus e RabbitMQ?

O desafio formal pede Azure Service Bus. Para manter a aderência e ainda permitir execução 100% local via Docker Compose:

- **Azure Service Bus**: transporte **primário** (para o cenário real/avaliativo em Azure)
- **RabbitMQ**: transporte **secundário** e **padrão no ambiente local** (compose), por simplicidade de setup sem depender de conta Azure

## Arquitetura (Mermaid)

```mermaid
flowchart LR
  UI[React Web] -->|REST| API[OrderManagement.Api]
  API -->|EF Core| PG[(PostgreSQL)]
  API -->|Publish OrderCreated| MQ[(Service Bus ou RabbitMQ)]
  WK[OrderManagement.Worker] -->|Consume OrderCreated| MQ
  WK -->|EF Core updates\n+ status history + idempotency| PG
```

## Fluxo de status

1) `POST /orders` cria um `Order` com status `Pending` e grava a primeira linha em `OrderStatusHistory`
2) A API publica uma mensagem no transporte configurado (`ORDER_MESSAGING_TRANSPORT`):
   - `CorrelationId = OrderId`
   - `EventType = "OrderCreated"` (RabbitMQ: `Type`; ASB: `Subject`/`EventType`)
   - `MessageId` gerado para idempotência
3) O worker consome a mensagem (idempotente por `MessageId` persistido em `ProcessedMessage`)
4) O worker atualiza o status:
   - `Pending -> Processing`
   - aguarda 5 segundos
   - `Processing -> Completed`
   - cada transição grava uma linha em `OrderStatusHistory`

Obs.: o tempo de espera pode ser configurado via `ORDER_STATUS_DELAY_SECONDS` (padrão: 5).

## Execução local (Docker Compose) — RabbitMQ (padrão)

Pré-requisitos:
- Docker Desktop

1) Crie o `.env` a partir do `.env.example` e ajuste se quiser.
   - Para rodar 100% local, mantenha `ORDER_MESSAGING_TRANSPORT=rabbitmq` (padrão).

2) Suba tudo:

```bash
docker compose --env-file .env up --build
```

Serviços:
- API: `http://localhost:8080` (Swagger em Development)
- Health: `http://localhost:8080/health`
- Frontend: `http://localhost:5173`
- PgAdmin: `http://localhost:5050`
- RabbitMQ Management: `http://localhost:15672` (user/pass padrão: `guest`/`guest`)

### Executando com Azure Service Bus (opcional)

Para usar Azure Service Bus de verdade, você precisa de uma **conta Azure** e de um **Service Bus Namespace** com uma **Queue**.

1) Crie um Namespace/Queue no Azure e copie a connection string (Shared Access Policy)
2) No `.env`, ajuste (exemplo):
   - `ORDER_MESSAGING_TRANSPORT=servicebus`
   - `AZURE_SERVICEBUS_CONNECTION_STRING=...`
   - `AZURE_SERVICEBUS_QUEUE_NAME=orders` (ou o nome da sua fila)

Obs.: nesse modo, o RabbitMQ pode ficar de pé (compose), mas não é usado (o transporte ativo é o Azure Service Bus).

Observações:
- A API aplica as migrations do EF Core automaticamente na inicialização.
- A fila `orders` é declarada automaticamente no RabbitMQ pela API/worker (caso não exista).

## Endpoints da API

### Criar pedido

`POST /orders`

Request (exemplo):

```json
{
  "customer": "Acme Ltda.",
  "product": "Produto X",
  "value": 99.90,
  "quantity": 2
}
```

### Listar pedidos

`GET /orders`

### Buscar pedido por id

`GET /orders/{id}`

## Estrutura do projeto

- `src/OrderManagement.Domain`: entities + enums
- `src/OrderManagement.Application`: DTOs + casos de uso (order service) + contratos de mensagem
- `src/OrderManagement.Infrastructure`: EF Core + publishers (RabbitMQ / Azure Service Bus) + DI
- `src/OrderManagement.Api`: controllers + middleware + health checks
- `src/OrderManagement.Worker`: consumers (RabbitMQ / Azure Service Bus) + idempotência + transições de status
- `web/order-management-web`: UI em React (Tailwind + polling)
