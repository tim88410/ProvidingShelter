namespace ProvidingShelter.Domain.Aggregates.DatasetAggregate;

public class Dataset
{
    // Aggregate Root
    public Guid Id { get; private set; } = Guid.NewGuid();

    // 以 dataset link 的最後一段數字作為資料集的自然鍵（例如 147094）
    public string DataId { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string Link { get; private set; } = default!;
    public DateOnly? OnshelfDate { get; private set; }
    public DateTime? UpdateDate { get; private set; }
    public string Provider { get; private set; } = default!;
    public string DataRange { get; private set; } = default!;
    public string? DataVersion { get; private set; }

    private readonly List<DatasetResource> _resources = new();
    public IReadOnlyCollection<DatasetResource> Resources => _resources;

    private Dataset() { } // EF

    public Dataset(
        string dataId,
        string title,
        string link,
        DateOnly? onshelfDate,
        DateTime? updateDate,
        string provider,
        string dataRange,
        string? dataVersion)
    {
        DataId = dataId;
        Title = title;
        Link = link;
        OnshelfDate = onshelfDate;
        UpdateDate = updateDate;
        Provider = provider;
        DataRange = dataRange;
        DataVersion = dataVersion;
    }

    public void UpsertResource(string dataName, string? extension, string fileUrl)
    {
        var existed = _resources.FirstOrDefault(r => r.DataName == dataName);
        if (existed is null)
        {
            _resources.Add(new DatasetResource(dataName, extension, fileUrl));
        }
        else
        {
            existed.UpdateUrl(fileUrl);
            if (!string.IsNullOrWhiteSpace(extension))
                existed.UpdateExtension(extension);
        }
    }
}
