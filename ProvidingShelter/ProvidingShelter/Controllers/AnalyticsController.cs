using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProvidingShelter.Application.Queries.Analytics.Aggregate;
using ProvidingShelter.Application.Queries.Analytics.Charts;
using ProvidingShelter.Application.Queries.Analytics.GeoScatter;
using ProvidingShelter.Application.Queries.Analytics.Hierarchy;
using ProvidingShelter.Application.Queries.Analytics.Meta;
using ProvidingShelter.Application.Queries.Analytics.Panels;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Web.Controllers
{
    [ApiController]
    [Route("api")]
    public sealed class AnalyticsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AnalyticsController(IMediator mediator) => _mediator = mediator;

        ///
        [HttpGet("meta/dimensions")]
        public async Task<ApiResult.Result> GetMeta()
            => await _mediator.Send(new GetAnalyticsMetaQuery());

        // ---- aggregate (series / heatmap) ----
        [HttpPost("aggregate")]
        public async Task<ApiResult.Result> Aggregate([FromBody] AggregateRequestDto body)
            => await _mediator.Send(new GetAggregateQuery { Body = body });

        // ---- charts: pie/donut ----
        [HttpPost("charts/pie")]
        public async Task<ApiResult.Result> Pie([FromBody] PieRequestDto body)
            => await _mediator.Send(new GetPieChartQuery { Body = body });

        // ---- charts: bar/line/area 快捷（轉呼叫 aggregate）----
        [HttpPost("charts/bar")]
        public Task<ApiResult.Result> Bar([FromBody] AggregateRequestDto body)
        {
            body.Output = "series";
            return _mediator.Send(new GetAggregateQuery { Body = body });
        }

        [HttpPost("charts/line")]
        public Task<ApiResult.Result> Line([FromBody] AggregateRequestDto body)
        {
            body.Output = "series";
            return _mediator.Send(new GetAggregateQuery { Body = body });
        }

        [HttpPost("charts/area")]
        public Task<ApiResult.Result> Area([FromBody] AggregateRequestDto body)
        {
            body.Output = "series";
            return _mediator.Send(new GetAggregateQuery { Body = body });
        }

        // ---- heatmap 快捷（轉呼叫 aggregate）----
        [HttpPost("charts/heatmap")]
        public Task<ApiResult.Result> Heatmap([FromBody] HeatmapRequestDto body)
        {
            var agg = new AggregateRequestDto
            {
                Filters = body.Filters,
                View = new ViewDto { Pivot = body.Pivot },
                Metric = body.Metric,
                Limit = body.Limit,
                Output = "heatmap"
            };
            return _mediator.Send(new GetAggregateQuery { Body = agg });
        }

        // ---- choropleth ----
        [HttpPost("charts/choropleth")]
        public async Task<ApiResult.Result> Choropleth([FromBody] ChoroplethRequestDto body)
            => await _mediator.Send(new GetChoroplethQuery { Body = body });

        // ---- scatter ----
        [HttpPost("charts/scatter")]
        public async Task<ApiResult.Result> Scatter([FromBody] ScatterRequestDto body)
            => await _mediator.Send(new GetScatterQuery { Body = body });

        // ---- hierarchy: sunburst/treemap ----
        [HttpPost("charts/sunburst")]
        public async Task<ApiResult.Result> Sunburst([FromBody] HierarchyRequestDto body)
            => await _mediator.Send(new GetHierarchyQuery { Body = body });

        [HttpPost("charts/treemap")]
        public async Task<ApiResult.Result> Treemap([FromBody] HierarchyRequestDto body)
            => await _mediator.Send(new GetHierarchyQuery { Body = body });
        /// <summary>一次取得「每縣市一張」的年×行業長條圖資料（面板）</summary>
        [HttpPost("charts/bar/panels")]
        public async Task<ApiResult.Result> BarPanels([FromBody] PanelRequestDto body)
            => await _mediator.Send(new GetPanelsQuery { Body = body });
    }
}
