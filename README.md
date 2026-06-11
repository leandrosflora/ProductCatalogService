# ProductCatalogService

Documentação do microserviço **ProductCatalogService** — serviço responsável pelo catálogo de produtos/SKUs usado por outros serviços da plataforma, especialmente no fluxo de promessa de entrega.

## Sumário

- [Visão geral](#visão-geral)
- [Responsabilidades](#responsabilidades)
- [O que este serviço não faz](#o-que-este-serviço-não-faz)
- [Stack técnica](#stack-técnica)
- [Arquitetura e organização do projeto](#arquitetura-e-organização-do-projeto)
- [Modelo de domínio](#modelo-de-domínio)
- [Configuração](#configuração)
- [Como executar localmente](#como-executar-localmente)
- [Health check e Swagger](#health-check-e-swagger)
- [Endpoints](#endpoints)
- [Endpoint batch para promessa de entrega](#endpoint-batch-para-promessa-de-entrega)
- [Cache Redis](#cache-redis)
- [Outbox de eventos](#outbox-de-eventos)
- [Persistência e índices](#persistência-e-índices)
- [Regras de negócio e validações](#regras-de-negócio-e-validações)
- [Fluxos principais](#fluxos-principais)
- [Testes manuais](#testes-manuais)
- [Comandos úteis](#comandos-úteis)

## Visão geral

O **ProductCatalogService** é o dono dos dados cadastrais, comerciais e físicos de produtos/SKUs. No contexto de uma arquitetura de microsserviços, ele centraliza as informações necessárias para que serviços consumidores consultem dados de catálogo sem manter cópias inconsistentes.

Um consumidor importante é o **Shipping Promise Service**, que precisa consultar peso, dimensões, categoria, fragilidade, restrições, seller e status dos SKUs para calcular promessa de entrega com baixa latência.

## Responsabilidades

Este microserviço é responsável por:

- Cadastrar produtos/SKUs.
- Consultar dados completos de um SKU.
- Consultar informações físicas de vários SKUs ativos em lote.
- Expor dados físicos e comerciais mínimos para cálculo de promessa de entrega.
- Atualizar peso, dimensões, fragilidade e restrição de um SKU.
- Alterar o status do produto.
- Invalidar o cache distribuído quando dados relevantes são alterados.
- Registrar eventos de alteração em uma tabela de outbox para publicação assíncrona.
- Expor health check para monitoramento operacional.

## O que este serviço não faz

Este serviço **não** é responsável por:

- Cálculo de frete.
- Cálculo de prazo de entrega.
- Gestão de estoque.
- Reserva de estoque.
- Criação ou fechamento de pedidos.
- Tracking de entregas.
- Integração direta com transportadoras.
- Publicação efetiva dos eventos da outbox para um broker externo.

## Stack técnica

- **.NET 8**
- **ASP.NET Core Minimal APIs**
- **Entity Framework Core 8**
- **PostgreSQL** com provider `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Redis** via `Microsoft.Extensions.Caching.StackExchangeRedis`
- **Swagger/OpenAPI** via `Swashbuckle.AspNetCore`
- **Health checks** do ASP.NET Core com verificação do `DbContext`

## Arquitetura e organização do projeto

O projeto segue uma separação simples por camadas:

```text
ProductCatalogService/
├── Api/
│   └── ProductEndpoints.cs
├── Application/
│   ├── Ports/
│   │   ├── IEventPublisher.cs
│   │   ├── IProductPhysicalInfoCache.cs
│   │   └── IProductRepository.cs
│   ├── ProductApplicationService.cs
│   └── ProductPhysicalInfoApplicationService.cs
├── Contracts/
│   ├── CreateProductRequest.cs
│   ├── PhysicalInfoContracts.cs
│   └── ProductResponse.cs
├── Domain/
│   ├── Product.cs
│   ├── ProductDimensions.cs
│   └── ProductStatus.cs
├── Infrastructure/
│   ├── Cache/
│   │   └── RedisProductPhysicalInfoCache.cs
│   ├── Outbox/
│   │   ├── OutboxEventPublisher.cs
│   │   └── OutboxMessage.cs
│   └── Persistence/
│       ├── ProductCatalogDbContext.cs
│       └── ProductRepository.cs
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── ProductCatalogService.http
```

### Camadas

| Camada | Responsabilidade |
|---|---|
| `Api` | Define os endpoints HTTP do microserviço. |
| `Application` | Orquestra casos de uso, cache, repositório e outbox. |
| `Application/Ports` | Define interfaces para persistência, cache e eventos. |
| `Contracts` | Define DTOs de entrada e saída da API. |
| `Domain` | Contém entidades, value objects, enumerações e validações de domínio. |
| `Infrastructure` | Implementa Redis, PostgreSQL/EF Core e outbox. |

## Modelo de domínio

### Produto

A entidade principal é `Product`. Ela representa um produto/SKU cadastrado no catálogo.

Campos principais:

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `Guid` | Identificador interno do produto. |
| `SellerId` | `Guid` | Identificador do seller responsável pelo SKU. |
| `SkuId` | `Guid` | Identificador externo do SKU. Deve ser único. |
| `Title` | `string` | Título do produto. |
| `Category` | `string` | Categoria do produto. |
| `Price` | `decimal` | Preço do produto. |
| `Status` | `ProductStatus` | Status atual do SKU. |
| `WeightKg` | `decimal` | Peso em quilogramas. |
| `Dimensions` | `ProductDimensions` | Altura, largura e comprimento em centímetros. |
| `IsFragile` | `bool` | Indica se o item é frágil. |
| `IsRestricted` | `bool` | Indica se o item possui restrição logística/comercial. |
| `CreatedAt` | `DateTimeOffset` | Data/hora de criação em UTC. |
| `UpdatedAt` | `DateTimeOffset` | Data/hora da última atualização em UTC. |

### Dimensões

`ProductDimensions` representa as dimensões físicas do produto:

| Campo | Tipo | Unidade |
|---|---|---|
| `HeightCm` | `decimal` | Centímetros |
| `WidthCm` | `decimal` | Centímetros |
| `LengthCm` | `decimal` | Centímetros |
| `VolumeCm3` | `decimal` | Centímetros cúbicos, calculado por `altura * largura * comprimento` |

### Status do produto

Valores aceitos para `ProductStatus`:

| Status | Valor numérico | Uso esperado |
|---|---:|---|
| `Draft` | 1 | SKU em rascunho. |
| `Active` | 2 | SKU ativo e elegível para consultas batch de informações físicas. |
| `Paused` | 3 | SKU pausado temporariamente. |
| `Blocked` | 4 | SKU bloqueado. |
| `Deleted` | 5 | SKU removido logicamente. |

> Observação: o endpoint batch de informações físicas retorna apenas produtos com status `Active` quando a busca precisa consultar o banco de dados.

## Configuração

As configurações ficam em `appsettings.json` e podem ser sobrescritas por `appsettings.Development.json` ou variáveis de ambiente.

### Connection strings

```json
{
  "ConnectionStrings": {
    "ProductCatalogDb": "Host=localhost;Port=5432;Database=product_catalog;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  }
}
```

| Chave | Descrição |
|---|---|
| `ConnectionStrings:ProductCatalogDb` | String de conexão do PostgreSQL usada pelo EF Core. |
| `ConnectionStrings:Redis` | Endereço do Redis usado pelo cache distribuído. |

### Variáveis de ambiente equivalentes

Em ambientes conteinerizados ou pipelines, é possível sobrescrever as configurações usando variáveis de ambiente:

```bash
ConnectionStrings__ProductCatalogDb="Host=postgres;Port=5432;Database=product_catalog;Username=postgres;Password=postgres"
ConnectionStrings__Redis="redis:6379"
ASPNETCORE_ENVIRONMENT="Development"
```

## Como executar localmente

### Pré-requisitos

- SDK do **.NET 8** instalado.

> Modo mock temporário: enquanto as instâncias de PostgreSQL e Redis não forem criadas, a API usa implementações em memória para repositório, cache e outbox. Portanto, não é necessário subir banco de dados nem Redis para executar os endpoints localmente.

### Dados mockados disponíveis

A aplicação sobe com alguns SKUs pré-cadastrados para facilitar testes de consulta, batch, atualização física e alteração de status:

| SKU | Seller | Produto | Status inicial |
|---|---|---|---|
| `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | `11111111-1111-1111-1111-111111111111` | Smartphone Mock 128GB | `Active` |
| `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | `22222222-2222-2222-2222-222222222222` | Tênis Mock Runner | `Active` |
| `cccccccc-cccc-cccc-cccc-cccccccccccc` | `33333333-3333-3333-3333-333333333333` | Perfume Mock 100ml | `Paused` |

Também é possível criar novos produtos com `POST /products/`; eles ficam disponíveis em memória enquanto a aplicação estiver rodando.

### Restaurar dependências

```bash
dotnet restore
```

### Compilar

```bash
dotnet build
```

### Executar a API

```bash
dotnet run
```

A aplicação usa as configurações definidas em `Properties/launchSettings.json` quando executada pelo perfil de desenvolvimento. O arquivo `ProductCatalogService.http` usa `http://localhost:5009` como host de exemplo.

## Health check e Swagger

### Health check

```http
GET /health
```

O health check valida a aplicação e inclui verificação do `ProductCatalogDbContext`.

### Swagger

Em ambiente `Development`, a aplicação habilita Swagger e Swagger UI.

URLs comuns em execução local:

```text
/swagger
/swagger/index.html
```

## Endpoints

Base path dos endpoints de produto:

```text
/products
```

### Criar produto/SKU

```http
POST /products/
Content-Type: application/json
```

#### Request

```json
{
  "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
  "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
  "title": "Smartphone demo",
  "category": "electronics",
  "price": 1299.90,
  "dimensions": {
    "heightCm": 10,
    "widthCm": 20,
    "lengthCm": 30
  },
  "weightKg": 1.25,
  "isFragile": false,
  "isRestricted": false
}
```

#### Response `201 Created`

```json
{
  "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
  "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
  "title": "Smartphone demo",
  "category": "electronics",
  "price": 1299.90,
  "status": "Active",
  "weightKg": 1.25,
  "heightCm": 10,
  "widthCm": 20,
  "lengthCm": 30,
  "isFragile": false,
  "isRestricted": false
}
```

#### Comportamento

- Valida se já existe produto para o mesmo `skuId`.
- Cria produto com status inicial `Active`.
- Registra evento `ProductCreated` na outbox.
- Persiste produto e evento na mesma unidade de trabalho do EF Core.

### Consultar produto por SKU

```http
GET /products/{skuId}
Accept: application/json
```

#### Response `200 OK`

```json
{
  "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
  "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
  "title": "Smartphone demo",
  "category": "electronics",
  "price": 1299.90,
  "status": "Active",
  "weightKg": 1.25,
  "heightCm": 10,
  "widthCm": 20,
  "lengthCm": 30,
  "isFragile": false,
  "isRestricted": false
}
```

#### Response `404 Not Found`

Retornado quando nenhum produto é encontrado para o `skuId` informado.

### Consultar informações físicas em lote

```http
POST /products/physical-info/batch
Content-Type: application/json
```

#### Request

```json
{
  "skuIds": [
    "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
    "74039986-a240-4cf8-9b2f-947d950f6cc2"
  ]
}
```

#### Response `200 OK`

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

#### Comportamento

- Remove SKUs duplicados da requisição.
- Busca primeiro no Redis.
- Consulta no PostgreSQL apenas os SKUs ausentes no cache.
- Retorna apenas SKUs ativos encontrados no banco.
- Salva no Redis os resultados obtidos no banco.
- Retorna lista vazia quando `skuIds` está vazio ou quando nenhum SKU ativo é encontrado.

### Atualizar informações físicas

```http
PUT /products/{skuId}/physical-info
Content-Type: application/json
```

#### Request

```json
{
  "weightKg": 1.50,
  "dimensions": {
    "heightCm": 11,
    "widthCm": 21,
    "lengthCm": 31
  },
  "isFragile": true,
  "isRestricted": false
}
```

#### Response `200 OK`

```json
{
  "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
  "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
  "title": "Smartphone demo",
  "category": "electronics",
  "price": 1299.90,
  "status": "Active",
  "weightKg": 1.50,
  "heightCm": 11,
  "widthCm": 21,
  "lengthCm": 31,
  "isFragile": true,
  "isRestricted": false
}
```

#### Comportamento

- Localiza produto por `skuId`.
- Atualiza peso, dimensões, fragilidade e restrição.
- Atualiza `UpdatedAt`.
- Registra evento `ProductPhysicalInfoChanged` na outbox.
- Remove a entrada de cache do SKU.
- Persiste alterações no PostgreSQL.

### Alterar status do produto

```http
PATCH /products/{skuId}/status
Content-Type: application/json
```

#### Request

```json
{
  "status": "Paused"
}
```

#### Response `200 OK`

```json
{
  "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
  "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
  "title": "Smartphone demo",
  "category": "electronics",
  "price": 1299.90,
  "status": "Paused",
  "weightKg": 1.50,
  "heightCm": 11,
  "widthCm": 21,
  "lengthCm": 31,
  "isFragile": true,
  "isRestricted": false
}
```

#### Comportamento

- Aceita os valores definidos em `ProductStatus`.
- O parse do status é case-insensitive.
- Registra evento `ProductStatusChanged` na outbox.
- Remove a entrada de cache do SKU.
- Persiste a alteração no PostgreSQL.

## Endpoint batch para promessa de entrega

O endpoint mais importante para integração com cálculo de promessa é:

```http
POST /products/physical-info/batch
```

Esse endpoint evita que o consumidor faça uma chamada por SKU, reduzindo latência, tráfego entre serviços e pressão sobre o banco de dados.

### Fluxo esperado com Shipping Promise

```text
Checkout Service
    ↓
Shipping Promise Service
    ↓
POST /products/physical-info/batch
    ↓
ProductCatalogService
    ↓
Redis cache
    ↓ cache miss
PostgreSQL
    ↓
Retorna peso, dimensões, categoria, seller, fragilidade, restrições e status
```

### Dados retornados para cálculo logístico

| Campo | Uso provável pelo consumidor |
|---|---|
| `skuId` | Correlacionar resposta com os itens solicitados. |
| `sellerId` | Identificar origem comercial/logística do item. |
| `weightKg` | Calcular peso total e regras de transportadora. |
| `heightCm`, `widthCm`, `lengthCm` | Calcular cubagem e restrições dimensionais. |
| `category` | Aplicar regras por categoria. |
| `isFragile` | Aplicar cuidados, transportadoras ou prazos específicos. |
| `isRestricted` | Bloquear ou restringir envio em certos cenários. |
| `status` | Confirmar elegibilidade do SKU. |

## Cache Redis

O serviço usa Redis para armazenar as respostas de informações físicas por SKU.

### Chave de cache

Formato lógico da chave:

```text
physical-info:{skuId sem hífens}
```

Exemplo:

```text
physical-info:2a85c8a7996d4bd4a1cb3f3b82fbcb53
```

A configuração do Redis define o prefixo de instância:

```text
product-catalog:
```

Na prática, a chave armazenada pelo provider pode ser prefixada por esse `InstanceName`.

### TTL

O TTL das informações físicas em cache é de **6 horas**.

### Invalidação

O cache é invalidado nos seguintes casos:

- Atualização de informações físicas (`PUT /products/{skuId}/physical-info`).
- Alteração de status (`PATCH /products/{skuId}/status`).

## Outbox de eventos

O serviço registra eventos em uma tabela de outbox, permitindo publicação assíncrona por um worker/processador externo.

### Eventos registrados

| Evento | Quando ocorre |
|---|---|
| `ProductCreated` | Após criação de um produto/SKU. |
| `ProductPhysicalInfoChanged` | Após atualização de peso, dimensões, fragilidade ou restrição. |
| `ProductStatusChanged` | Após alteração do status do SKU. |

### Payload dos eventos

Os eventos incluem dados relevantes do produto:

```json
{
  "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
  "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
  "category": "electronics",
  "weightKg": 1.25,
  "heightCm": 10,
  "widthCm": 20,
  "lengthCm": 30,
  "isFragile": false,
  "isRestricted": false,
  "status": "Active",
  "updatedAt": "2026-06-10T12:00:00+00:00"
}
```

### Tabela `outbox_messages`

| Coluna | Tipo lógico | Descrição |
|---|---|---|
| `id` | `Guid` | Identificador da mensagem. |
| `event_type` | `string` | Tipo do evento. |
| `payload` | `jsonb` | Payload serializado em JSON. |
| `created_at` | `DateTimeOffset` | Data/hora de criação. |
| `processed_at` | `DateTimeOffset?` | Data/hora de processamento, quando houver. |

> Importante: este projeto registra mensagens na outbox, mas não contém o worker responsável por publicar essas mensagens em um broker.

## Persistência e índices

A persistência é feita com EF Core usando PostgreSQL.

### Tabela `products`

Mapeamento lógico dos principais campos:

| Coluna | Origem no domínio | Observação |
|---|---|---|
| `id` | `Product.Id` | Chave primária. |
| `sku_id` | `Product.SkuId` | Obrigatório e único. |
| `seller_id` | `Product.SellerId` | Obrigatório. |
| `title` | `Product.Title` | Obrigatório, até 300 caracteres. |
| `category` | `Product.Category` | Obrigatório, até 100 caracteres. |
| `price` | `Product.Price` | Precisão `18,2`. |
| `weight_kg` | `Product.WeightKg` | Precisão `10,3`. |
| `status` | `Product.Status` | Armazenado como string, até 30 caracteres. |
| `height_cm` | `Product.Dimensions.HeightCm` | Precisão `10,2`. |
| `width_cm` | `Product.Dimensions.WidthCm` | Precisão `10,2`. |
| `length_cm` | `Product.Dimensions.LengthCm` | Precisão `10,2`. |
| `is_fragile` | `Product.IsFragile` | Booleano. |
| `is_restricted` | `Product.IsRestricted` | Booleano. |
| `created_at` | `Product.CreatedAt` | Data/hora de criação. |
| `updated_at` | `Product.UpdatedAt` | Data/hora da última atualização. |

### Índices

| Índice | Objetivo |
|---|---|
| `SkuId` único | Impedir dois produtos com o mesmo SKU. |
| `SellerId + Status` | Apoiar consultas por seller e status. |
| `Category` | Apoiar consultas e filtros por categoria. |
| `ProcessedAt` na outbox | Apoiar busca de mensagens pendentes/processadas. |

## Regras de negócio e validações

### Produto

- `SellerId` não pode ser `Guid.Empty`.
- `SkuId` não pode ser `Guid.Empty`.
- `Title` é obrigatório e é salvo com `Trim()`.
- `Category` é obrigatória e é salva com `Trim()`.
- `Price` não pode ser negativo.
- `WeightKg` deve ser maior que zero.
- Produto novo começa com status `Active`.

### Dimensões

- `HeightCm` deve ser maior que zero.
- `WidthCm` deve ser maior que zero.
- `LengthCm` deve ser maior que zero.

### Status

- O status informado deve existir no enum `ProductStatus`.
- A alteração de status atualiza `UpdatedAt`.
- A alteração de status invalida o cache de informações físicas do SKU.

### Informações físicas

- Atualizar informações físicas também atualiza `UpdatedAt`.
- Peso e dimensões devem continuar positivos.
- A alteração invalida o cache de informações físicas do SKU.

## Fluxos principais

### Criação de produto

```text
Cliente
  ↓ POST /products/
API Minimal Endpoint
  ↓
ProductApplicationService.CreateAsync
  ↓ verifica SKU existente
ProductRepository.GetBySkuIdAsync
  ↓ cria entidade de domínio Product
ProductRepository.AddAsync
  ↓ registra ProductCreated
OutboxEventPublisher.AddToOutboxAsync
  ↓ salva produto + outbox
ProductRepository.SaveChangesAsync
  ↓
201 Created
```

### Consulta batch de informações físicas

```text
Cliente/Shipping Promise Service
  ↓ POST /products/physical-info/batch
ProductPhysicalInfoApplicationService.GetBatchAsync
  ↓ remove duplicados
RedisProductPhysicalInfoCache.GetAsync por SKU
  ↓ cache miss
ProductRepository.GetBySkuIdsAsync
  ↓ filtra apenas ProductStatus.Active
Mapeia ProductPhysicalInfoResponse
  ↓ salva cada SKU encontrado no Redis com TTL de 6 horas
200 OK
```

### Atualização de informações físicas

```text
Cliente
  ↓ PUT /products/{skuId}/physical-info
ProductApplicationService.UpdatePhysicalInfoAsync
  ↓ busca produto por SKU
Product.UpdatePhysicalInfo
  ↓ registra ProductPhysicalInfoChanged
OutboxEventPublisher.AddToOutboxAsync
  ↓ remove cache do SKU
RedisProductPhysicalInfoCache.RemoveAsync
  ↓ salva alterações
ProductRepository.SaveChangesAsync
  ↓
200 OK
```

### Alteração de status

```text
Cliente
  ↓ PATCH /products/{skuId}/status
ProductApplicationService.ChangeStatusAsync
  ↓ busca produto por SKU
Enum.TryParse<ProductStatus>(ignoreCase: true)
  ↓ altera status
Product.ChangeStatus
  ↓ registra ProductStatusChanged
OutboxEventPublisher.AddToOutboxAsync
  ↓ remove cache do SKU
RedisProductPhysicalInfoCache.RemoveAsync
  ↓ salva alterações
ProductRepository.SaveChangesAsync
  ↓
200 OK
```

## Testes manuais

O arquivo `ProductCatalogService.http` contém exemplos de chamadas HTTP para:

- Criar produto.
- Consultar produto por SKU.
- Consultar informações físicas em lote.
- Atualizar informações físicas.
- Alterar status.

Em editores compatíveis, como Visual Studio, JetBrains Rider ou VS Code com extensão REST Client, execute as requisições diretamente pelo arquivo `.http`.

### Exemplo com `curl`

```bash
curl -X POST http://localhost:5009/products/ \
  -H "Content-Type: application/json" \
  -d '{
    "sellerId": "d8017639-4f75-4701-9915-f5757b6234a8",
    "skuId": "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53",
    "title": "Smartphone demo",
    "category": "electronics",
    "price": 1299.90,
    "dimensions": {
      "heightCm": 10,
      "widthCm": 20,
      "lengthCm": 30
    },
    "weightKg": 1.25,
    "isFragile": false,
    "isRestricted": false
  }'
```

```bash
curl -X POST http://localhost:5009/products/physical-info/batch \
  -H "Content-Type: application/json" \
  -d '{
    "skuIds": [
      "2a85c8a7-996d-4bd4-a1cb-3f3b82fbcb53"
    ]
  }'
```

## Comandos úteis

### Restaurar pacotes

```bash
dotnet restore
```

### Compilar solução

```bash
dotnet build ProductCatalogService.sln
```

### Executar serviço

```bash
dotnet run --project ProductCatalogService.csproj
```

### Executar em ambiente de desenvolvimento

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ProductCatalogService.csproj
```

## Observações operacionais

- Garanta que PostgreSQL e Redis estejam disponíveis antes de subir a API.
- O health check depende do banco configurado em `ProductCatalogDb`.
- Não há migrations versionadas no repositório atualmente; caso o projeto passe a usar migrations, recomenda-se versioná-las junto ao código.
- O padrão outbox exige um processador externo para publicar e marcar mensagens como processadas.
- O cache melhora a latência do endpoint batch, mas o dado canônico permanece no PostgreSQL.
