# ProductCatalogService

Product Catalog Service owns product/SKU data for shipping promise flows. It exposes fast lookup APIs for SKU physical information used by downstream services such as Shipping Promise Service.

## Main endpoints

- `POST /products/` creates a product/SKU.
- `GET /products/{skuId}` returns complete catalog data for one SKU.
- `POST /products/physical-info/batch` returns weight, dimensions, category, seller, fragility, restriction and status data for many active SKUs.
- `PUT /products/{skuId}/physical-info` updates physical data and invalidates the Redis cache entry.
- `PATCH /products/{skuId}/status` changes SKU status and invalidates the Redis cache entry.
- `GET /health` checks service health, including the EF Core database context.

## Infrastructure

The service uses ASP.NET Core Minimal APIs, EF Core with PostgreSQL, Redis through `IDistributedCache`, and an outbox table for product events.
