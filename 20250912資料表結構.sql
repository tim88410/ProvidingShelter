/* 切到目標 DB（請先確定已存在） */
IF DB_ID(N'ProvidingShelter') IS NULL
BEGIN
    RAISERROR(N'Database [ProvidingShelter] not found.',16,1);
    RETURN;
END
GO
USE [ProvidingShelter];
GO

/* ========== Dataset ========== */
IF OBJECT_ID(N'[dbo].[Dataset]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Dataset](
        [Id]                UNIQUEIDENTIFIER NOT NULL,
        [ContactName]       NVARCHAR(100) NULL,
        [ContactPhone]      NVARCHAR(100) NULL,
        [DatasetId]         NVARCHAR(50)  NOT NULL,
        [DatasetName]       NVARCHAR(500) NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [DownloadUrls]      NVARCHAR(MAX) NULL,
        [Encoding]          NVARCHAR(MAX) NULL,
        [FileFormats]       NVARCHAR(4000) NULL,
        [LastImportedAt]    DATETIME2 NOT NULL,
        [License]           NVARCHAR(100) NULL,
        [MainFieldDescription] NVARCHAR(MAX) NULL,
        [Note]              NVARCHAR(MAX) NULL,
        [OnshelfDate]       DATE NULL,
        [PageUrl]           NVARCHAR(1000) NULL,
        [Pricing]           NVARCHAR(100) NULL,
        [Provider]          NVARCHAR(200) NULL,
        [ProviderAttribute] NVARCHAR(200) NULL,
        [PublishMethod]     NVARCHAR(100) NULL,
        [QualityCheck]      NVARCHAR(200) NULL,
        [RelatedUrls]       NVARCHAR(MAX) NULL,
        [ServiceCategory]   NVARCHAR(200) NULL,
        [UpdateDate]        DATETIME2 NULL,
        [UpdateFrequency]   NVARCHAR(100) NULL,
        CONSTRAINT [PK_Dataset] PRIMARY KEY ([Id])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Dataset_DatasetId' AND object_id=OBJECT_ID(N'[dbo].[Dataset]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Dataset_DatasetId] ON [dbo].[Dataset]([DatasetId]);
END
GO

/* ========== DatasetResource ========== */
IF OBJECT_ID(N'[dbo].[DatasetResource]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DatasetResource](
        [DatasetId]                NVARCHAR(50)  NOT NULL,
        [ResourceKey]              NVARCHAR(100) NOT NULL,
        [AccessURL]                NVARCHAR(1000) NULL,
        [Checksum]                 NVARCHAR(64)  NULL,
        [Description]              NVARCHAR(MAX) NULL,
        [DescriptionEn]            NVARCHAR(MAX) NULL,
        [DownloadURL]              NVARCHAR(1000) NULL,
        [ETag]                     NVARCHAR(200) NULL,
        [FieldDesc]                NVARCHAR(MAX) NULL,
        [FieldDescEn]              NVARCHAR(MAX) NULL,
        [Format]                   NVARCHAR(50)  NULL,
        [IsApiLike]                BIT NOT NULL,
        [LastKnownSavedSizeBytes]  BIGINT NULL,
        [LastKnownWireSizeBytes]   BIGINT NULL,
        [LastModified]             DATETIME2 NULL,
        [MediaType]                NVARCHAR(255) NULL,
        [Status]                   TINYINT NOT NULL CONSTRAINT [DF_DatasetResource_Status] DEFAULT ((0)),
        [Title]                    NVARCHAR(500) NULL,
        [UpdatedAtUtc]             DATETIME2 NOT NULL CONSTRAINT [DF_DatasetResource_UpdatedAtUtc] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_DatasetResource] PRIMARY KEY ([DatasetId],[ResourceKey])
    );
END
GO

/* ========== DatasetResourceFetch ========== */
IF OBJECT_ID(N'[dbo].[DatasetResourceFetch]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DatasetResourceFetch](
        [Id]               BIGINT IDENTITY(1,1) NOT NULL,
        [ContentType]      NVARCHAR(255) NULL,
        [Converter]        NVARCHAR(100) NULL,
        [DatasetId]        NVARCHAR(50)  NOT NULL,
        [DetectedFormat]   NVARCHAR(50)  NULL,
        [ETag]             NVARCHAR(200) NULL,
        [Encoding]         NVARCHAR(50)  NULL,
        [Error]            NVARCHAR(MAX) NULL,
        [FetchAtUtc]       DATETIME2 NOT NULL,
        [HttpStatus]       INT NULL,
        [LastModified]     DATETIME2 NULL,
        [Ok]               BIT NOT NULL,
        [ResourceKey]      NVARCHAR(100) NOT NULL,
        [SavedPath]        NVARCHAR(1024) NULL,
        [SavedSizeBytes]   BIGINT NULL,
        [WireSizeBytes]    BIGINT NULL,
        CONSTRAINT [PK_DatasetResourceFetch] PRIMARY KEY ([Id])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_DatasetResourceFetch_DatasetId_ResourceKey' AND object_id=OBJECT_ID(N'[dbo].[DatasetResourceFetch]'))
BEGIN
    CREATE INDEX [IX_DatasetResourceFetch_DatasetId_ResourceKey]
        ON [dbo].[DatasetResourceFetch]([DatasetId],[ResourceKey]);
END
GO

/* ========== DatasetResourceContent ========== */
IF OBJECT_ID(N'[dbo].[DatasetResourceContent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DatasetResourceContent](
        [DatasetId]       NVARCHAR(50)  NOT NULL,
        [ResourceKey]     NVARCHAR(100) NOT NULL,
        [ContentHash]     NVARCHAR(64)  NULL,
        [ContentJson]     NVARCHAR(MAX) NULL,
        [ContentPath]     NVARCHAR(1024) NULL,
        [ConvertedAtUtc]  DATETIME2 NOT NULL,
        [JsonSizeBytes]   BIGINT NULL,
        [SavedSizeBytes]  BIGINT NULL,
        [StorageMode]     NVARCHAR(10)  NOT NULL,
        [WireSizeBytes]   BIGINT NULL,
        CONSTRAINT [PK_DatasetResourceContent] PRIMARY KEY ([DatasetId],[ResourceKey])
    );
END
GO

/* ========== SexualAssaultInformation（Keyless，無主鍵） ========== */
IF OBJECT_ID(N'[dbo].[SexualAssaultInformation]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SexualAssaultInformation](
        [OwnerCityCode]   NVARCHAR(20) NULL,
        [OccurCity]       NVARCHAR(20) NULL,
        [OccurTown]       NVARCHAR(20) NULL,
        [TownCode]        NVARCHAR(20) NULL,
        [InfoerType]      NVARCHAR(20) NULL,
        [InfoUnit]        NVARCHAR(20) NULL,
        [GENDER]          NVARCHAR(20) NULL,
        [IdType]          NVARCHAR(20) NULL,
        [Occupation]      NVARCHAR(20) NULL,
        [Education]       NVARCHAR(20) NULL,
        [School]          NVARCHAR(20) NULL,
        [DSexId]          NVARCHAR(20) NULL,
        [Relation]        NVARCHAR(20) NULL,
        [OccurPlace]      NVARCHAR(20) NULL,
        [Maimed]          NVARCHAR(100) NULL,
        [ClientId]        NVARCHAR(50) NULL,
        [DId]             NVARCHAR(50) NULL,
        [OtherInfoerType] NVARCHAR(200) NULL,
        [OtherInfoUnit]   NVARCHAR(200) NULL,
        [OtherOccupation] NVARCHAR(200) NULL,
        [OtherMaimed]     NVARCHAR(200) NULL,
        [OtherMaimed2]    NVARCHAR(200) NULL,
        [OtherRelation]   NVARCHAR(200) NULL,
        [OtherOccurPlace] NVARCHAR(200) NULL,
        [BDate]           INT NULL,
        [DBDate]          INT NULL,
        [NumOfSuspect]    TINYINT NULL,
        [LastOccurTime]   DATETIME2 NULL,
        [InfoTimeYear]    SMALLINT NULL,
        [InfoTimeMonth]   TINYINT NULL,
        [ReceiveTime]     DATETIME2 NULL,
        [NotifyDate]      INT NULL
        -- 無主鍵（EF Keyless）
    );
END
GO

/* ========== SexualAssaultImport ========== */
IF OBJECT_ID(N'[dbo].[SexualAssaultImport]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SexualAssaultImport](
        [Id]              UNIQUEIDENTIFIER NOT NULL,
        [SourceFileName]  NVARCHAR(255) NOT NULL,
        [StoredFullPath]  NVARCHAR(500) NOT NULL,
        [FileHashSha256]  NVARCHAR(64)  NOT NULL,
        [CrossTableTitle] NVARCHAR(200) NOT NULL,
        [ImportedAtUtc]   DATETIME2 NOT NULL CONSTRAINT [DF_SexualAssaultImport_ImportedAtUtc] DEFAULT (GETUTCDATE()),
        [PeriodYearStart] INT NULL,
        [PeriodYearEnd]   INT NULL,
        [CategoryType]    TINYINT NOT NULL,
        [RawRowCount]     INT NOT NULL,
        [ParsedRowCount]  INT NOT NULL,
        CONSTRAINT [PK_SexualAssaultImport] PRIMARY KEY ([Id])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_SexualAssaultImport_FileHashSha256' AND object_id=OBJECT_ID(N'[dbo].[SexualAssaultImport]'))
BEGIN
    CREATE UNIQUE INDEX [IX_SexualAssaultImport_FileHashSha256]
        ON [dbo].[SexualAssaultImport]([FileHashSha256]);
END
GO

/* ========== SexualAssaultStat ========== */
IF OBJECT_ID(N'[dbo].[SexualAssaultStat]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SexualAssaultStat](
        [Id]             UNIQUEIDENTIFIER NOT NULL,
        [ImportId]       UNIQUEIDENTIFIER NOT NULL,
        [Year]           INT NOT NULL,
        [CityCode]       NVARCHAR(10) NOT NULL,
        [CityName]       NVARCHAR(20) NOT NULL,
        [Nationality]    TINYINT NOT NULL,
        [CategoryType]   TINYINT NOT NULL,
        [CategoryKey]    NVARCHAR(50) NOT NULL,
        [CategoryNameZh] NVARCHAR(50) NOT NULL,
        [Count]          INT NOT NULL,
        [IsTotalRow]     BIT NOT NULL CONSTRAINT [DF_SexualAssaultStat_IsTotalRow] DEFAULT ((0)),
        [CreatedAtUtc]   DATETIME2 NOT NULL CONSTRAINT [DF_SexualAssaultStat_CreatedAtUtc] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_SexualAssaultStat] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SexualAssaultStat_SexualAssaultImport_ImportId]
            FOREIGN KEY ([ImportId]) REFERENCES [dbo].[SexualAssaultImport]([Id]) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_SexualAssaultStat_ImportId' AND object_id=OBJECT_ID(N'[dbo].[SexualAssaultStat]'))
BEGIN
    CREATE INDEX [IX_SexualAssaultStat_ImportId] ON [dbo].[SexualAssaultStat]([ImportId]);
END
GO

/* ========== RisCityCode ========== */
IF OBJECT_ID(N'[dbo].[RisCityCode]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RisCityCode](
        [CityCode]    NVARCHAR(10)  NOT NULL,
        [CityName]    NVARCHAR(20)  NOT NULL,
        [IsCurrent]   BIT NULL,
        [ResourceUrl] NVARCHAR(200) NOT NULL,
        CONSTRAINT [PK_RisCityCode] PRIMARY KEY ([CityCode])
    );
END
GO
