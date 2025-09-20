using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Domain.Repositories
{
    public interface ISexualAssaultAnalyticsQueries
    {
        Task<DimensionsMetaDto> GetDimensionsMetaAsync(CancellationToken ct = default);

        Task<SeriesResultDto> GetSeriesAsync(AggregateRequestDto request, CancellationToken ct = default);

        Task<PieResultDto> GetPieAsync(PieRequestDto request, CancellationToken ct = default);

        Task<HeatmapResultDto> GetHeatmapAsync(HeatmapRequestDto request, CancellationToken ct = default);

        Task<List<ChoroplethFeatureDto>> GetChoroplethAsync(ChoroplethRequestDto request, CancellationToken ct = default);

        Task<ScatterResultDto> GetScatterAsync(ScatterRequestDto request, CancellationToken ct = default);

        Task<HierarchyResultDto> GetHierarchyAsync(HierarchyRequestDto request, CancellationToken ct = default);
        Task<PanelResultDto> GetSeriesPanelsAsync(PanelRequestDto request, CancellationToken ct = default);
    }
}
