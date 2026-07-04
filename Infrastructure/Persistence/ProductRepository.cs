using Dapper;
using ProductCatalogService.Application.Ports;
using ProductCatalogService.Domain;

namespace ProductCatalogService.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private const string ProductColumns = """
        id as Id,
        seller_id as SellerId,
        sku_id as SkuId,
        title as Title,
        category as Category,
        price as Price,
        status as Status,
        weight_kg as WeightKg,
        height_cm as HeightCm,
        width_cm as WidthCm,
        length_cm as LengthCm,
        is_fragile as IsFragile,
        is_restricted as IsRestricted,
        image_url as ImageUrl,
        created_at as CreatedAt,
        updated_at as UpdatedAt
        """;

    private readonly ProductCatalogDbConnectionFactory _connectionFactory;
    private readonly ProductCatalogUnitOfWork _unitOfWork;

    public ProductRepository(
        ProductCatalogDbConnectionFactory connectionFactory,
        ProductCatalogUnitOfWork unitOfWork)
    {
        _connectionFactory = connectionFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<Product?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken)
    {
        const string sql = $"""
            select {ProductColumns}
            from products
            where sku_id = @SkuId
            limit 1;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { SkuId = skuId }, cancellationToken: cancellationToken);
        var record = await connection.QuerySingleOrDefaultAsync<ProductRecord>(command);
        var product = record?.ToDomain();

        if (product is not null)
        {
            _unitOfWork.Track(product);
        }

        return product;
    }

    public async Task<IReadOnlyList<Product>> GetBySkuIdsAsync(
        IReadOnlyCollection<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        if (skuIds.Count == 0)
        {
            return [];
        }

        const string sql = $"""
            select {ProductColumns}
            from products
            where sku_id = any(@SkuIds);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                SkuIds = skuIds.ToArray(),
                Status = ProductStatus.Active.ToString()
            },
            cancellationToken: cancellationToken);

        var records = await connection.QueryAsync<ProductRecord>(command);
        var products = records.Select(x => x.ToDomain()).ToList();

        foreach (var product in products)
        {
            _unitOfWork.Track(product);
        }

        return products;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = $"""
            select {ProductColumns}
            from products
            order by updated_at desc;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var records = await connection.QueryAsync<ProductRecord>(command);
        var products = records.Select(x => x.ToDomain()).ToList();

        foreach (var product in products)
        {
            _unitOfWork.Track(product);
        }

        return products;
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _unitOfWork.Add(product);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        const string insertProductSql = """
            insert into products (
                id,
                seller_id,
                sku_id,
                title,
                category,
                price,
                status,
                weight_kg,
                height_cm,
                width_cm,
                length_cm,
                is_fragile,
                is_restricted,
                image_url,
                created_at,
                updated_at
            )
            values (
                @Id,
                @SellerId,
                @SkuId,
                @Title,
                @Category,
                @Price,
                @Status,
                @WeightKg,
                @HeightCm,
                @WidthCm,
                @LengthCm,
                @IsFragile,
                @IsRestricted,
                @ImageUrl,
                @CreatedAt,
                @UpdatedAt
            );
            """;

        const string updateProductSql = """
            update products
            set seller_id = @SellerId,
                sku_id = @SkuId,
                title = @Title,
                category = @Category,
                price = @Price,
                status = @Status,
                weight_kg = @WeightKg,
                height_cm = @HeightCm,
                width_cm = @WidthCm,
                length_cm = @LengthCm,
                is_fragile = @IsFragile,
                is_restricted = @IsRestricted,
                image_url = @ImageUrl,
                updated_at = @UpdatedAt
            where id = @Id;
            """;

        const string insertOutboxSql = """
            insert into outbox_messages (
                id,
                event_type,
                payload,
                created_at,
                processed_at
            )
            values (
                @Id,
                @EventType,
                cast(@Payload as jsonb),
                @CreatedAt,
                @ProcessedAt
            );
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var product in _unitOfWork.NewProducts)
            {
                await connection.ExecuteAsync(ToCommand(insertProductSql, product, transaction, cancellationToken));
            }

            foreach (var product in _unitOfWork.TrackedProducts)
            {
                await connection.ExecuteAsync(ToCommand(updateProductSql, product, transaction, cancellationToken));
            }

            foreach (var message in _unitOfWork.OutboxMessages)
            {
                var command = new CommandDefinition(
                    insertOutboxSql,
                    message,
                    transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(command);
            }

            await transaction.CommitAsync(cancellationToken);
            _unitOfWork.Clear();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static CommandDefinition ToCommand(
        string sql,
        Product product,
        System.Data.IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            sql,
            new
            {
                product.Id,
                product.SellerId,
                product.SkuId,
                product.Title,
                product.Category,
                product.Price,
                Status = product.Status.ToString(),
                product.WeightKg,
                product.Dimensions.HeightCm,
                product.Dimensions.WidthCm,
                product.Dimensions.LengthCm,
                product.IsFragile,
                product.IsRestricted,
                product.ImageUrl,
                product.CreatedAt,
                product.UpdatedAt
            },
            transaction,
            cancellationToken: cancellationToken);
    }
}
