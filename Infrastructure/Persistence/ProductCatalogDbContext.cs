using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Domain;
using ProductCatalogService.Infrastructure.Outbox;

namespace ProductCatalogService.Infrastructure.Persistence;

public sealed class ProductCatalogDbContext : DbContext
{
    public ProductCatalogDbContext(DbContextOptions<ProductCatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.SkuId)
                .HasColumnName("sku_id")
                .IsRequired();

            entity.Property(x => x.SellerId)
                .HasColumnName("seller_id")
                .IsRequired();

            entity.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.Category)
                .HasColumnName("category")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Price)
                .HasColumnName("price")
                .HasPrecision(18, 2);

            entity.Property(x => x.WeightKg)
                .HasColumnName("weight_kg")
                .HasPrecision(10, 3);

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(x => x.IsFragile)
                .HasColumnName("is_fragile");

            entity.Property(x => x.IsRestricted)
                .HasColumnName("is_restricted");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            entity.OwnsOne(x => x.Dimensions, dimensions =>
            {
                dimensions.Property(x => x.HeightCm)
                    .HasColumnName("height_cm")
                    .HasPrecision(10, 2);

                dimensions.Property(x => x.WidthCm)
                    .HasColumnName("width_cm")
                    .HasPrecision(10, 2);

                dimensions.Property(x => x.LengthCm)
                    .HasColumnName("length_cm")
                    .HasPrecision(10, 2);
            });

            entity.HasIndex(x => x.SkuId)
                .IsUnique();

            entity.HasIndex(x => new { x.SellerId, x.Status });

            entity.HasIndex(x => x.Category);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(x => x.ProcessedAt)
                .HasColumnName("processed_at");

            entity.HasIndex(x => x.ProcessedAt);
        });
    }
}
