# ProductCatalogService

Microservice de catálogo de produtos/SKUs da arquitetura Logística Envios Demo.

Este serviço expõe somente os dados logísticos de produtos ativos necessários para consumidores como o serviço de promessa de entrega. Ele não calcula frete, não calcula prazo, não gerencia estoque, não cria pedidos e não publica eventos Kafka.

## Responsabilidades

- Consultar dados logísticos de um SKU ativo.
- Consultar dados logísticos de múltiplos SKUs ativos em lote.
- Retornar peso, dimensões, categoria, seller e códigos de restrição logística.
- Usar cache para reduzir leituras repetidas dos dados logísticos.
- Expor health check operacional.

## API pública

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/v1/products/{skuId}/logistics` | Consulta dados logísticos de um SKU ativo. |
| `GET` | `/v1/products/logistics/batch?skuIds={skuId}&skuIds={skuId}` | Consulta dados logísticos de múltiplos SKUs ativos. |

### `GET /v1/products/{skuId}/logistics`

Retorna `404 Not Found` quando o SKU não existe ou não está ativo.

Exemplo de resposta:

```json
{
  "skuId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "sellerId": "11111111-1111-1111-1111-111111111111",
  "weightKg": 0.19,
  "heightCm": 15.8,
  "widthCm": 7.5,
  "lengthCm": 0.9,
  "category": "Electronics",
  "restrictionCodes": ["FRAGILE"]
}
```

### `GET /v1/products/logistics/batch`

Recebe o parâmetro de query `skuIds` com uma ou mais ocorrências:

```http
GET /v1/products/logistics/batch?skuIds=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&skuIds=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb
```

Retorna somente SKUs encontrados e ativos.

## Stack técnica

- .NET 8
- ASP.NET Core Minimal APIs
- Entity Framework Core 8
- PostgreSQL
- Redis
- Swagger/OpenAPI
- Health Checks

## Organização

```text
ProductCatalogService/
├── Api/
├── Application/
│   └── Ports/
├── Contracts/
├── Domain/
├── Infrastructure/
│   ├── Cache/
│   ├── Mocking/
│   ├── Outbox/
│   └── Persistence/
├── Program.cs
└── ProductCatalogService.csproj
```

## Dados mockados

Em modo local, a aplicação usa repositório e cache em memória.

| SKU | Seller | Produto | Status inicial |
|---|---|---|---|
| `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | `11111111-1111-1111-1111-111111111111` | Smartphone Mock 128GB | `Active` |
| `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | `22222222-2222-2222-2222-222222222222` | Tênis Mock Runner | `Active` |
| `cccccccc-cccc-cccc-cccc-cccccccccccc` | `33333333-3333-3333-3333-333333333333` | Perfume Mock 100ml | `Paused` |

## Como executar

```bash
dotnet restore
dotnet run
```

Swagger fica disponível em ambiente de desenvolvimento.

## Health check

```http
GET /health
```
