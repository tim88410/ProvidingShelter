using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.DatasetCatalog;

namespace ProvidingShelter.Infrastructure.Persistence;

public class ShelterDbContext : DbContext
{
    public ShelterDbContext(DbContextOptions<ShelterDbContext> options) : base(options) { }

    //public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetCatalog> DatasetCatalogs => Set<DatasetCatalog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatasetCatalog>(b =>
        {
            b.ToTable("DatasetCatalogs");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.DatasetId).IsUnique();

            b.Property(x => x.DatasetId).IsRequired().HasMaxLength(50);
            b.Property(x => x.DatasetName).HasMaxLength(500);
            b.Property(x => x.ProviderAttribute).HasMaxLength(200);
            b.Property(x => x.ServiceCategory).HasMaxLength(200);
            b.Property(x => x.QualityCheck).HasMaxLength(200);
            b.Property(x => x.FileFormats).HasMaxLength(4000); // 可選
            b.Property(x => x.DownloadUrls);
            b.Property(x => x.Encoding).HasColumnType("nvarchar(max)");
            b.Property(x => x.PublishMethod).HasMaxLength(100);
            b.Property(x => x.Description);
            b.Property(x => x.MainFieldDescription);
            b.Property(x => x.Provider).HasMaxLength(200);
            b.Property(x => x.UpdateFrequency).HasMaxLength(100);
            b.Property(x => x.License).HasMaxLength(100);
            b.Property(x => x.RelatedUrls);
            b.Property(x => x.Pricing).HasMaxLength(100);
            b.Property(x => x.ContactName).HasMaxLength(100);
            b.Property(x => x.ContactPhone).HasMaxLength(100);
            b.Property(x => x.PageUrl).HasMaxLength(1000);
            b.Property(x => x.LastImportedAt).IsRequired();
        });
    }
}
