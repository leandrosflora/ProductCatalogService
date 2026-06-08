# ProductCatalogService

O **Product Catalog Service** é o dono dos dados de produto/SKU. No contexto de promessa de entrega, ele fornece ao **Shipping Promise Service** as informações físicas e comerciais mínimas necessárias para calcular uma promessa com baixa latência.

## Responsabilidades

Este serviço é responsável por:

- Cadastrar produtos/SKUs.
- Consultar dados completos de um SKU.
- Expor peso, dimensões, categoria, fragilidade, restrições, seller e status.
- Consultar informações físicas de vários SKUs em lote.
- Atualizar peso, dimensões, fragilidade e restrições.
- Alterar status do produto.
- Invalidar cache de leitura quando dados relevantes mudam.
- Registrar eventos de alteração em outbox para publicação assíncrona.

Este serviço **não** é responsável por cálculo de frete, cálculo de prazo, estoque, reserva de estoque, criação de pedido, tracking ou integração com transportadoras.

## Endpoints principais

- `POST /products/` cria um produto/SKU.
- `GET /products/{skuId}` retorna os dados completos de catálogo para um SKU.
- `POST /products/physical-info/batch` retorna peso, dimensões, categoria, seller, fragilidade, restrição e status de vários SKUs ativos em uma única chamada.
- `PUT /products/{skuId}/physical-info` atualiza dados físicos e invalida a entrada correspondente no Redis.
- `PATCH /products/{skuId}/status` altera o status do SKU e invalida a entrada correspondente no Redis.
- `GET /health` verifica a saúde do serviço, incluindo o contexto EF Core do banco de dados.

## Endpoint batch para Shipping Promise

O endpoint mais importante para integrações com promessa de entrega é:

```http
POST /products/physical-info/batch
```

Ele evita que o consumidor faça uma chamada por SKU e reduz latência e tráfego entre serviços.

### Exemplo de requisição

```json
{
  "skuIds": [
    "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
    "74039986-a240-4cf8-9b2f-947d950f6cc2"
  ]
}
```

### Exemplo de resposta

```json
[
  {
    "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
    "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
    "weightKg": 1.25,
    "heightCm": 10,
    "widthCm": 20,
    "lengthCm": 30,
    "category": "electronics",
    "isFragile": false,
    "isRestricted": false,
    "status": "Active"
  }
]
```

## Infraestrutura

A implementação usa:

- **ASP.NET Core Minimal APIs** para endpoints HTTP enxutos.
- **EF Core** com **PostgreSQL** para persistência dos produtos e mensagens de outbox.
- **Redis** via `IDistributedCache` para cache distribuído das informações físicas do SKU.
- **Outbox pattern** para registrar eventos como `ProductCreated`, `ProductPhysicalInfoChanged` e `ProductStatusChanged`.
- **Health checks** para validar dependências do serviço.

## Regras importantes

- `sku_id` possui índice único para impedir dois produtos com o mesmo SKU.
- O endpoint batch retorna apenas produtos com status `Active`.
- Dados físicos usam cache com TTL de 6 horas.
- Atualizações de dados físicos e status invalidam o cache do SKU.
- Alterações relevantes são registradas no outbox para posterior publicação de eventos.

## Fluxo esperado com Shipping Promise

```text
Checkout Service
    ↓
Shipping Promise Service
    ↓
POST /products/physical-info/batch
    ↓
Product Catalog Service
    ↓
Redis cache
    ↓ cache miss
PostgreSQL
    ↓
Retorna peso, dimensões, categoria, seller, fragilidade, restrições e status
```

## Configuração local

As connection strings ficam em `appsettings.json` e `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "ProductCatalogDb": "Host=localhost;Port=5432;Database=product_catalog;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  }
}
```

## Como executar

Com o SDK do .NET instalado e as dependências PostgreSQL/Redis disponíveis:

```bash
dotnet restore
dotnet run
```

A API expõe Swagger em ambiente de desenvolvimento.
