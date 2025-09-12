using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.DatasetAggregate;
using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;
using ProvidingShelter.Domain.Entities;
using ProvidingShelter.Infrastructure.Persistence.Models;

namespace ProvidingShelter.Infrastructure.Persistence;

public class ShelterDbContext : DbContext
{
    public ShelterDbContext(DbContextOptions<ShelterDbContext> options) : base(options) { }

    public DbSet<Dataset> Datasets => Set<Dataset>();

    public DbSet<DatasetResource> DatasetResources => Set<DatasetResource>();
    public DbSet<DatasetResourceFetch> DatasetResourceFetches => Set<DatasetResourceFetch>();
    public DbSet<DatasetResourceContent> DatasetResourceContents => Set<DatasetResourceContent>();

    public DbSet<SexualAssaultInformation> SexualAssaultInformations => Set<SexualAssaultInformation>();

    // ★ 新增
    public DbSet<SexualAssaultImport> SexualAssaultImports => Set<SexualAssaultImport>();
    public DbSet<SexualAssaultStat> SexualAssaultStats => Set<SexualAssaultStat>();
    public DbSet<RisCityCode> RisCityCodes => Set<RisCityCode>();

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

            b.Property<string?>(nameof(DatasetResourceContent.ContentJson))
             .HasColumnName("ContentJson")
             .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<SexualAssaultInformation>(b =>
        {
            b.ToTable(nameof(SexualAssaultInformation));
            b.HasNoKey();

            b.Property(x => x.OwnerCityCode).HasMaxLength(20);
            b.Property(x => x.OccurCity).HasMaxLength(20);
            b.Property(x => x.OccurTown).HasMaxLength(20);
            b.Property(x => x.TownCode).HasMaxLength(20);

            b.Property(x => x.InfoerType).HasMaxLength(20);
            b.Property(x => x.InfoUnit).HasMaxLength(20);
            b.Property(x => x.Gender).HasMaxLength(20).HasColumnName("GENDER");
            b.Property(x => x.IdType).HasMaxLength(20);
            b.Property(x => x.Occupation).HasMaxLength(20);
            b.Property(x => x.Education).HasMaxLength(20);
            b.Property(x => x.School).HasMaxLength(20);
            b.Property(x => x.DSexId).HasMaxLength(20);
            b.Property(x => x.Relation).HasMaxLength(20);
            b.Property(x => x.OccurPlace).HasMaxLength(20);

            b.Property(x => x.Maimed).HasMaxLength(100);

            b.Property(x => x.ClientId).HasMaxLength(50);
            b.Property(x => x.DId).HasMaxLength(50);

            b.Property(x => x.OtherInfoerType).HasMaxLength(200);
            b.Property(x => x.OtherInfoUnit).HasMaxLength(200);
            b.Property(x => x.OtherOccupation).HasMaxLength(200);
            b.Property(x => x.OtherMaimed).HasMaxLength(200);
            b.Property(x => x.OtherMaimed2).HasMaxLength(200);
            b.Property(x => x.OtherRelation).HasMaxLength(200);
            b.Property(x => x.OtherOccurPlace).HasMaxLength(200);

            b.Property(x => x.BDate);
            b.Property(x => x.DBDate);
            b.Property(x => x.NumOfSuspect);

            b.Property(x => x.LastOccurTime);

            b.Property(x => x.InfoTimeYear);
            b.Property(x => x.InfoTimeMonth);

            b.Property(x => x.ReceiveTime);
            b.Property(x => x.NotifyDate);
        });

        modelBuilder.Entity<SexualAssaultImport>(b =>
        {
            b.ToTable(nameof(SexualAssaultImport));
            b.HasKey(x => x.Id);

            b.Property(x => x.SourceFileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.StoredFullPath).IsRequired().HasMaxLength(500);
            b.Property(x => x.FileHashSha256).IsRequired().HasMaxLength(64);
            b.Property(x => x.CrossTableTitle).IsRequired().HasMaxLength(200);

            b.Property(x => x.ImportedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            b.Property(x => x.PeriodYearStart);
            b.Property(x => x.PeriodYearEnd);

            // Enum → tinyint
            b.Property(x => x.CategoryType).HasConversion<byte>();

            b.Property(x => x.RawRowCount);
            b.Property(x => x.ParsedRowCount);

            b.HasIndex(x => x.FileHashSha256).IsUnique(); // 去重匯入
        });

        modelBuilder.Entity<SexualAssaultStat>(b =>
        {
            b.ToTable(nameof(SexualAssaultStat));
            b.HasKey(x => x.Id);

            b.Property(x => x.ImportId).IsRequired();

            b.Property(x => x.Year).IsRequired();
            b.Property(x => x.CityCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.CityName).IsRequired().HasMaxLength(20);

            b.Property(x => x.Nationality).HasConversion<byte>();
            b.Property(x => x.CategoryType).HasConversion<byte>();

            b.Property(x => x.CategoryKey).IsRequired().HasMaxLength(50);
            b.Property(x => x.CategoryNameZh).IsRequired().HasMaxLength(50);

            b.Property(x => x.Count).IsRequired();
            b.Property(x => x.IsTotalRow).HasDefaultValue(false);

            b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            b.HasOne<SexualAssaultImport>()
             .WithMany(i => i.Stats)
             .HasForeignKey(x => x.ImportId);
        });

        modelBuilder.Entity<RisCityCode>(b =>
        {
            b.ToTable(nameof(RisCityCode));
            b.HasKey(x => x.CityCode);

            b.Property(x => x.ResourceUrl).IsRequired().HasMaxLength(200);
            b.Property(x => x.CityCode).IsRequired().HasMaxLength(10);
            b.Property(x => x.CityName).IsRequired().HasMaxLength(20);
            b.Property(x => x.IsCurrent);
        });
    }
}
