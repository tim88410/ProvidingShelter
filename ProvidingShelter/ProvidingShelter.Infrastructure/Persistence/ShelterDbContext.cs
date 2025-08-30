using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.DatasetAggregate;
using ProvidingShelter.Domain.Aggregates.DatasetCatalog;

namespace ProvidingShelter.Infrastructure.Persistence;

public class ShelterDbContext : DbContext
{
    public ShelterDbContext(DbContextOptions<ShelterDbContext> options) : base(options) { }

    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetResource> DatasetResources => Set<DatasetResource>();
    public DbSet<DatasetCatalog> DatasetCatalogs => Set<DatasetCatalog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dataset>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.DataId).IsUnique();

            b.Property(x => x.Title).IsRequired().HasMaxLength(500);
            b.Property(x => x.Link).IsRequired().HasMaxLength(1000);
            b.Property(x => x.Provider).HasMaxLength(200);
            b.Property(x => x.DataRange).HasMaxLength(1000);
            b.Property(x => x.DataVersion).HasMaxLength(300);

            // ★ 用導覽屬性 Resources 建立一對多，並指定 FK
            b.HasMany(x => x.Resources)
             .WithOne()
             .HasForeignKey(r => r.DatasetId)
             .OnDelete(DeleteBehavior.Cascade);

            // ★ 告訴 EF：Resources 的 backing field 是 "_resources"
            b.Navigation(x => x.Resources)
             .HasField("_resources")
             .UsePropertyAccessMode(PropertyAccessMode.Field); // 用欄位讀寫
        });

        modelBuilder.Entity<DatasetResource>(b =>
        {
            b.HasKey(x => x.Id);

            // ★ 明確宣告 FK 欄位（若你已在上方 HasForeignKey 指定，這裡可不寫，但保留屬性長度設定）
            b.Property(x => x.DatasetId).IsRequired();

            b.Property(x => x.DataName).IsRequired().HasMaxLength(300);
            b.Property(x => x.Extension).HasMaxLength(20);
            b.Property(x => x.FileUrl).IsRequired().HasMaxLength(2000);
            b.Property(x => x.DownloadedPath).HasMaxLength(2000);
        });

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
