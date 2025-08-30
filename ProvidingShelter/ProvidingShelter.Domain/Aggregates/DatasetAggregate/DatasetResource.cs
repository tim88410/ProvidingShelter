namespace ProvidingShelter.Domain.Aggregates.DatasetAggregate;

public class DatasetResource
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DatasetId { get; private set; }
    public string DataName { get; private set; } = default!;
    public string? Extension { get; private set; }
    public string FileUrl { get; private set; } = default!;
    public string? DownloadedPath { get; private set; }

    private DatasetResource() { } // EF

    public DatasetResource(string dataName, string? extension, string fileUrl)
    {
        DataName = dataName;
        Extension = extension;
        FileUrl = fileUrl;
    }

    public void MarkDownloaded(string localPath) => DownloadedPath = localPath;
    public void UpdateUrl(string newUrl) => FileUrl = newUrl;
    public void UpdateExtension(string? ext) => Extension = ext;
}

