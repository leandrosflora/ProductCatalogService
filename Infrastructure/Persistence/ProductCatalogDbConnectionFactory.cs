using Npgsql;

namespace ProductCatalogService.Infrastructure.Persistence;

public sealed class ProductCatalogDbConnectionFactory
{
    private readonly string _connectionString;

    public ProductCatalogDbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ProductCatalogDb")
            ?? throw new InvalidOperationException("Connection string 'ProductCatalogDb' was not configured.");
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
