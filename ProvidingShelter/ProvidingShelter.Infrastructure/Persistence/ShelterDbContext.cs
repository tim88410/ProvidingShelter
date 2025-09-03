using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.DatasetAggregate;
using ProvidingShelter.Infrastructure.Persistence.Models;

namespace ProvidingShelter.Infrastructure.Persistence;

public class ShelterDbContext : DbContext
{
    public ShelterDbContext(DbContextOptions<ShelterDbContext> options) : base(options) { }

    public DbSet<Dataset> Datasets => Set<Dataset>();
    
    public DbSet<DatasetResource> DatasetResources => Set<DatasetResource>();
    public DbSet<DatasetResourceFetch> DatasetResourceFetches => Set<DatasetResourceFetch>();
    public DbSet<DatasetResourceContent> DatasetResourceContents => Set<DatasetResourceContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Dataset>(b =>
        {
            b.ToTable(nameof(Dataset));
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

        // DatasetResource
        modelBuilder.Entity<DatasetResource>(b =>
        {
            b.ToTable(nameof(DatasetResource));
            b.HasKey(x => new { x.DatasetId, x.ResourceKey });
            b.Property(x => x.DatasetId).HasMaxLength(50);
            b.Property(x => x.ResourceKey).HasMaxLength(100);
            b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.Format).HasMaxLength(50);
            b.Property(x => x.MediaType).HasMaxLength(255);
            b.Property(x => x.AccessURL).HasMaxLength(1000);
            b.Property(x => x.DownloadURL).HasMaxLength(1000);
            b.Property(x => x.ETag).HasMaxLength(200);
            b.Property(x => x.Checksum).HasMaxLength(64);
            b.Property(x => x.Status).HasDefaultValue((byte)0);
            b.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        });

        // DatasetResourceFetch
        modelBuilder.Entity<DatasetResourceFetch>(b =>
        {
            b.ToTable(nameof(DatasetResourceFetch));
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.DatasetId, x.ResourceKey });
            b.Property(x => x.DatasetId).HasMaxLength(50);
            b.Property(x => x.ResourceKey).HasMaxLength(100);
            b.Property(x => x.ContentType).HasMaxLength(255);
            b.Property(x => x.Encoding).HasMaxLength(50);
            b.Property(x => x.ETag).HasMaxLength(200);
            b.Property(x => x.SavedPath).HasMaxLength(1024);
            b.Property(x => x.DetectedFormat).HasMaxLength(50);
            b.Property(x => x.Converter).HasMaxLength(100);
        });

        // DatasetResourceContent
        modelBuilder.Entity<DatasetResourceContent>(b =>
        {
            b.ToTable(nameof(DatasetResourceContent));
            b.HasKey(x => new { x.DatasetId, x.ResourceKey });
            b.Property(x => x.DatasetId).HasMaxLength(50);
            b.Property(x => x.ResourceKey).HasMaxLength(100);
            b.Property(x => x.StorageMode).HasMaxLength(10);
            b.Property(x => x.ContentPath).HasMaxLength(1024);
            b.Property(x => x.ContentHash).HasMaxLength(64);
        });
    }
}
