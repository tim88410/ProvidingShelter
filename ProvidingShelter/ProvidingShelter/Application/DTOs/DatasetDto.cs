namespace ProvidingShelter.Application.DTOs;

public sealed class DatasetDto
{
    public string DataId { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Link { get; init; } = default!;
    public DateOnly? OnshelfDate { get; init; }
    public DateTime? UpdateDate { get; init; }
    public string Provider { get; init; } = default!;
    public string DataRange { get; init; } = default!;
    public string? DataVersion { get; init; }
}
