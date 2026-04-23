# Gestão de Pedidos (Teste Técnico)

MVP de um sistema de gestão de pedidos com:

- API: .NET 8 Web API + EF Core
- DB: PostgreSQL
- Mensageria: Azure Service Bus (integração real via variáveis de ambiente)
- Worker: consome eventos `OrderCreated` e atualiza o status de forma assíncrona
- Frontend: React + Vite + Tailwind (UI com polling)
- Infra: Docker + Docker Compose (api, worker, frontend, postgres, pgadmin)

## Architecture (Mermaid)

```mermaid
flowchart LR
  UI[React Web] -->|REST| API[OrderManagement.Api]
  API -->|EF Core| PG[(PostgreSQL)]
  API -->|Publish OrderCreated| ASB[(Azure Service Bus Queue)]
  WK[OrderManagement.Worker] -->|Consume OrderCreated| ASB
  WK -->|EF Core updates\n+ status history + idempotency| PG
```

## Fluxo de status

1) `POST /orders` cria um `Order` com status `Pending` e grava a primeira linha em `OrderStatusHistory`
2) A API publica uma mensagem no Service Bus:
   - `CorrelationId = OrderId`
   - `ApplicationProperties["EventType"] = "OrderCreated"`
3) O worker consome a mensagem (idempotente por `MessageId` persistido em `ProcessedMessage`)
4) O worker atualiza o status:
   - `Pending -> Processing`
   - aguarda 5 segundos
   - `Processing -> Completed`
   - cada transição grava uma linha em `OrderStatusHistory`

## Execução local (Docker Compose)

Pré-requisitos:
- Docker Desktop
- Transporte de mensageria:
  - Padrão: outbox no Postgres (não precisa de Azure)
  - Opcional: Azure Service Bus (ver abaixo)

1) Crie o `.env` a partir do `.env.example` e preencha:
- `ORDER_MESSAGING_TRANSPORT` (`outbox` or `servicebus`)
- se `servicebus`:
  - `AZURE_SERVICE_BUS_CONNECTION_STRING`
  - opcionalmente `AZURE_SERVICE_BUS_QUEUE_NAME` (padrão: `orders`)

2) Suba tudo:

```bash
docker compose --env-file .env up --build
```

Serviços:
- API: `http://localhost:8080` (Swagger em Development)
- Health: `http://localhost:8080/health`
- Frontend: `http://localhost:5173`
- PgAdmin: `http://localhost:5050`

Observações:
- A API aplica as migrations do EF Core automaticamente na inicialização.
- Se estiver usando Service Bus, a API tenta criar a fila se as credenciais tiverem permissão de gerenciamento.

## Configuração do Azure Service Bus

Você precisa de uma fila e uma connection string:
- Recomendado: use uma SAS policy com `Send` para a API e `Listen` para o worker.
- Se você não tiver permissão de gerenciamento, crie a fila manualmente no Azure Portal.

Variáveis de ambiente usadas:
- `AZURE_SERVICE_BUS_CONNECTION_STRING` (necessária para o fluxo assíncrono ponta-a-ponta)
- `AZURE_SERVICE_BUS_QUEUE_NAME` (padrão: `orders`)

## Endpoints da API

### Criar pedido

`POST /orders`

Request (exemplo):

```json
{
  "customer": "Acme Inc.",
  "product": "Widget",
  "value": 99.90
}
```

Response (201) (exemplo):

```json
{
  "id": "b3b6c61e-7bb8-4c88-99e7-0b0bf2c5e3e1",
  "customer": "Acme Inc.",
  "product": "Widget",
  "value": 99.90,
  "status": "Pending",
  "createdAt": "2026-04-21T01:23:45.678Z",
  "updatedAt": null,
  "statusHistory": [
    {
      "id": "f5a8d0c0-1b4b-4b98-9d1e-4ac2b5a48ed1",
      "previousStatus": null,
      "newStatus": "Pending",
      "changedAt": "2026-04-21T01:23:45.678Z",
      "source": "api"
    }
  ]
}
```

### Listar pedidos

`GET /orders`

### Buscar pedido por id

`GET /orders/{id}`

## Estrutura do projeto

- `src/OrderManagement.Domain`: entities + enums
- `src/OrderManagement.Application`: DTOs + casos de uso (order service) + contratos de mensagem
- `src/OrderManagement.Infrastructure`: EF Core + publisher do Service Bus + DI
- `src/OrderManagement.Api`: controllers + middleware + health checks
- `src/OrderManagement.Worker`: consumer do Service Bus + idempotência + transições de status
- `web/order-management-web`: UI em React (Tailwind + polling)
