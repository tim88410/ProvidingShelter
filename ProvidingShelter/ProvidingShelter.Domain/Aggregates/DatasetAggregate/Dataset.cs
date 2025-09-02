namespace ProvidingShelter.Domain.Aggregates.DatasetAggregate
{
    public class Dataset
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string DatasetId { get; private set; } = default!;
        public string? DatasetName { get; private set; }
        public string? ProviderAttribute { get; private set; }
        public string? ServiceCategory { get; private set; }
        public string? QualityCheck { get; private set; }
        public string? FileFormats { get; private set; }
        public string? DownloadUrls { get; private set; }
        public string? Encoding { get; private set; }
        public string? PublishMethod { get; private set; }
        public string? Description { get; private set; }
        public string? MainFieldDescription { get; private set; }
        public string? Provider { get; private set; }
        public string? UpdateFrequency { get; private set; }
        public string? License { get; private set; }
        public string? RelatedUrls { get; private set; }
        public string? Pricing { get; private set; }
        public string? ContactName { get; private set; }
        public string? ContactPhone { get; private set; }
        public DateOnly? OnshelfDate { get; private set; }
        public DateTime? UpdateDate { get; private set; }
        public string? Note { get; private set; }
        public string? PageUrl { get; private set; }
        public DateTime LastImportedAt { get; private set; }

        private Dataset() { }

        public Dataset(string datasetId) => DatasetId = datasetId;

        public void Upsert(
            string? datasetName, string? providerAttribute, string? serviceCategory, string? qualityCheck,
            string? fileFormats, string? downloadUrls, string? encoding, string? publishMethod,
            string? description, string? mainFieldDescription, string? provider, string? updateFrequency,
            string? license, string? relatedUrls, string? pricing, string? contactName, string? contactPhone,
            DateOnly? onshelfDate, DateTime? updateDate, string? note, string? pageUrl, DateTime importedAt)
        {
            DatasetName = datasetName;
            ProviderAttribute = providerAttribute;
            ServiceCategory = serviceCategory;
            QualityCheck = qualityCheck;
            FileFormats = fileFormats;
            DownloadUrls = downloadUrls;
            Encoding = encoding;
            PublishMethod = publishMethod;
            Description = description;
            MainFieldDescription = mainFieldDescription;
            Provider = provider;
            UpdateFrequency = updateFrequency;
            License = license;
            RelatedUrls = relatedUrls;
            Pricing = pricing;
            ContactName = contactName;
            ContactPhone = contactPhone;
            OnshelfDate = onshelfDate;
            UpdateDate = updateDate;
            Note = note;
            PageUrl = pageUrl;
            LastImportedAt = importedAt;
        }
    }
}
