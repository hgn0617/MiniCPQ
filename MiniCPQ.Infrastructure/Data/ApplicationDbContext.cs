using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Identity;

namespace MiniCPQ.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerMaterial> ServerMaterials => Set<ServerMaterial>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<QuoteMaterialSnapshot> QuoteMaterialSnapshots => Set<QuoteMaterialSnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Material>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Materials_UnitPrice", "\"UnitPrice\" >= 0"));
        });

        builder.Entity<Server>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<ServerMaterial>(entity =>
        {
            entity.HasKey(x => new { x.ServerId, x.MaterialId });
            entity.HasOne(x => x.Server)
                .WithMany(x => x.Materials)
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Material)
                .WithMany(x => x.ServerMaterials)
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_ServerMaterials_Quantity", "\"Quantity\" > 0"));
        });

        builder.Entity<Quote>(entity =>
        {
            entity.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Cost).HasPrecision(18, 2);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Quotes_Cost", "\"Cost\" >= 0");
                t.HasCheckConstraint("CK_Quotes_Price", "\"Price\" >= 0");
            });
        });

        builder.Entity<QuoteItem>(entity =>
        {
            entity.Property(x => x.ServerNameSnapshot).HasMaxLength(100);
            entity.Property(x => x.UnitCostSnapshot).HasPrecision(18, 2);
            entity.Property(x => x.TotalCostSnapshot).HasPrecision(18, 2);
            entity.HasOne(x => x.Quote)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Server)
                .WithMany(x => x.QuoteItems)
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.ToTable(t => t.HasCheckConstraint("CK_QuoteItems_Quantity", "\"Quantity\" > 0"));
        });

        builder.Entity<QuoteMaterialSnapshot>(entity =>
        {
            entity.Property(x => x.MaterialName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MaterialType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.TotalPrice).HasPrecision(18, 2);
            entity.HasOne(x => x.QuoteItem)
                .WithMany(x => x.MaterialSnapshots)
                .HasForeignKey(x => x.QuoteItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_QuoteMaterialSnapshots_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint("CK_QuoteMaterialSnapshots_Prices", "\"UnitPrice\" >= 0 AND \"TotalPrice\" >= 0");
            });
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RefreshMaterialVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RefreshMaterialVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RefreshMaterialVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Material>().Where(x => x.State == EntityState.Modified))
        {
            entry.Entity.Version = Guid.NewGuid();
        }
    }
}
