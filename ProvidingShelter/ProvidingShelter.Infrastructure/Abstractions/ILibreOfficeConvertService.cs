namespace ProvidingShelter.Infrastructure.Abstractions
{
    public interface ILibreOfficeConvertService
    {
        Task<string> ConvertOdsToXlsxAsync(string odsFullPath, CancellationToken ct = default);
    }
}
