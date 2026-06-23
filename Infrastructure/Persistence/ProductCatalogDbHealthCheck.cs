using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductCatalogService.Infrastructure.Persistence;

public sealed class ProductCatalogDbHealthCheck : IHealthCheck
{
    private readonly ProductCatalogDbConnectionFactory _connectionFactory;

    public ProductCatalogDbHealthCheck(ProductCatalogDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition("select 1;", cancellationToken: cancellationToken);
            await connection.ExecuteScalarAsync<int>(command);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Product catalog database is unavailable.", ex);
        }
    }
}
