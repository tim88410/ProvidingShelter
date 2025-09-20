using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;
using ProvidingShelter.Domain.DTOs;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Repositories
{
    public sealed class SexualAssaultAnalyticsQueryRepository : ISexualAssaultAnalyticsQueries
    {
        private readonly ShelterDbContext _db;
        public SexualAssaultAnalyticsQueryRepository(ShelterDbContext db) => _db = db;

        // ===== META =====
        public async Task<DimensionsMetaDto> GetDimensionsMetaAsync(CancellationToken ct = default)
        {
            var q = _db.SexualAssaultStats.AsNoTracking()
                     .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            var years = await q.Select(s => s.Year).Distinct().OrderBy(y => y).ToListAsync(ct);
            var cities = await q.Select(s => s.CityName).Distinct().OrderBy(s => s).ToListAsync(ct);
            var nations = await q.Select(s => s.Nationality).Distinct().ToListAsync(ct);
            var inds = await q.Select(s => s.CategoryNameZh).Distinct().OrderBy(s => s).ToListAsync(ct);

            return new DimensionsMetaDto
            {
                Years = years,
                Cities = cities,
                Nationalities = nations.Select(DisplayNationality).Distinct().OrderBy(s => s).ToList(),
                Industries = inds
            };
        }

        // ===== SERIES / LINE / AREA =====
        public async Task<SeriesResultDto> GetSeriesAsync(AggregateRequestDto request, CancellationToken ct = default)
        {
            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            // 先在資料庫取回必要欄位，再在記憶體分組
            var rows = await baseQ.Select(s => new Row
            {
                Year = s.Year,
                City = s.CityName,
                Nat = s.Nationality,
                Ind = s.CategoryNameZh,
                Val = s.Count
            }).ToListAsync(ct);

            Func<Row, string> xKey = request.View.X switch
            {
                DimensionKey.year => r => r.Year.ToString(),
                DimensionKey.city => r => r.City,
                DimensionKey.nationality => r => DisplayNationality(r.Nat),
                _ => r => r.Ind
            };

            Func<Row, string>? sKey = (request.View.Series is null || request.View.Series == DimensionKey.none)
                ? null
                : request.View.Series switch
                {
                    DimensionKey.year => r => r.Year.ToString(),
                    DimensionKey.city => r => r.City,
                    DimensionKey.nationality => r => DisplayNationality(r.Nat),
                    _ => r => r.Ind
                };

            var categories = rows.Select(xKey).Distinct().OrderBy(x => x).ToList();
            var series = new List<SeriesLineDto>();

            if (sKey == null)
            {
                var agg = rows.GroupBy(xKey).ToDictionary(g => g.Key, g => (double)g.Sum(x => x.Val));
                var data = categories.Select(cat => agg.TryGetValue(cat, out var v) ? v : 0.0).ToList();
                series.Add(new SeriesLineDto { Name = "total", Data = data });
            }
            else
            {
                var seriesNames = rows.Select(sKey).Distinct().OrderBy(x => x).ToList();
                var agg = rows.GroupBy(r => new { X = xKey(r), S = sKey(r) })
                              .ToDictionary(g => (g.Key.X, g.Key.S), g => (double)g.Sum(x => x.Val));

                foreach (var sn in seriesNames)
                {
                    var data = categories.Select(cat => agg.TryGetValue((cat, sn), out var v) ? v : 0.0).ToList();
                    series.Add(new SeriesLineDto { Name = sn, Data = data });
                }
            }

            // 移動平均（僅 x=year）
            if (request.Metric.MovingAverage > 0 && request.View.X == DimensionKey.year)
            {
                int k = Math.Max(1, request.Metric.MovingAverage);
                foreach (var line in series) line.Data = MovingAverage(line.Data, k);
            }

            // 百分比（grand total）
            if (request.Metric.Normalize == NormalizeMode.percent)
            {
                double grand = series.SelectMany(s => s.Data).Sum();
                if (grand > 0)
                {
                    foreach (var s in series) s.Data = s.Data.Select(v => v / grand).ToList();
                }
            }

            return new SeriesResultDto
            {
                Meta = new { x = request.View.X.ToString(), series = request.View.Series?.ToString(), stack = request.View.Stack, filtersApplied = request.Filters },
                Categories = categories,
                Series = series,
                Unit = request.Metric.Normalize == NormalizeMode.percent ? "%" : "人"
            };
        }

        // ===== PIE / DONUT =====
        public async Task<PieResultDto> GetPieAsync(PieRequestDto request, CancellationToken ct = default)
        {
            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            var rows = await baseQ.Select(s => new Row
            {
                Year = s.Year,
                City = s.CityName,
                Nat = s.Nationality,
                Ind = s.CategoryNameZh,
                Val = s.Count
            }).ToListAsync(ct);

            Func<Row, string> dim = request.Dimension switch
            {
                DimensionKey.year => r => r.Year.ToString(),
                DimensionKey.city => r => r.City,
                DimensionKey.nationality => r => DisplayNationality(r.Nat),
                _ => r => r.Ind
            };

            var grouped = rows.GroupBy(dim).Select(g => new { Key = g.Key, Val = (double)g.Sum(x => x.Val) });
            var ordered = request.Limit?.SortBy == "label"
                ? grouped.OrderBy(x => x.Key)
                : grouped.OrderByDescending(x => x.Val);

            List<(string key, double val)> buckets;
            if (request.Limit?.TopN is int top && top > 0)
            {
                var topItems = ordered.Take(top).Select(x => (x.Key, x.Val)).ToList();
                if (request.Limit.OtherBucket)
                {
                    var otherVal = ordered.Skip(top).Sum(x => x.Val);
                    if (otherVal > 0) topItems.Add(("其他", otherVal));
                }
                buckets = topItems;
            }
            else
            {
                buckets = ordered.Select(x => (x.Key, x.Val)).ToList();
            }

            double total = buckets.Sum(x => x.val);
            var items = buckets.Select(x => new PieItemDto
            {
                Label = x.key,
                Value = x.val,
                Percentage = total > 0 ? x.val / total : (double?)null
            }).ToList();

            return new PieResultDto
            {
                Meta = new { dimension = request.Dimension.ToString(), filtersApplied = request.Filters },
                Items = items,
                Unit = request.Metric.Normalize == NormalizeMode.percent ? "%" : "人"
            };
        }

        // ===== HEATMAP =====
        public async Task<HeatmapResultDto> GetHeatmapAsync(HeatmapRequestDto request, CancellationToken ct = default)
        {
            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            var rows = await baseQ.Select(s => new Row
            {
                Year = s.Year,
                City = s.CityName,
                Nat = s.Nationality,
                Ind = s.CategoryNameZh,
                Val = s.Count
            }).ToListAsync(ct);

            Func<Row, string> rKey = request.Pivot.Rows switch
            {
                DimensionKey.year => r => r.Year.ToString(),
                DimensionKey.city => r => r.City,
                DimensionKey.nationality => r => DisplayNationality(r.Nat),
                _ => r => r.Ind
            };
            Func<Row, string> cKey = request.Pivot.Cols switch
            {
                DimensionKey.year => r => r.Year.ToString(),
                DimensionKey.city => r => r.City,
                DimensionKey.nationality => r => DisplayNationality(r.Nat),
                _ => r => r.Ind
            };

            var rowNames = rows.Select(rKey).Distinct().OrderBy(x => x).ToList();
            var colNames = rows.Select(cKey).Distinct().OrderBy(x => x).ToList();

            var agg = rows.GroupBy(r => new { R = rKey(r), C = cKey(r) })
                          .ToDictionary(g => (g.Key.R, g.Key.C), g => (double)g.Sum(x => x.Val));

            var matrix = rowNames
                .Select(r => colNames.Select(c => agg.TryGetValue((r, c), out var v) ? v : 0.0).ToList())
                .ToList();

            if (request.Metric.Normalize == NormalizeMode.percent)
            {
                var grand = matrix.Sum(row => row.Sum());
                if (grand > 0) matrix = matrix.Select(row => row.Select(v => v / grand).ToList()).ToList();
            }

            return new HeatmapResultDto
            {
                Meta = new { rows = request.Pivot.Rows.ToString(), cols = request.Pivot.Cols.ToString(), filtersApplied = request.Filters },
                Rows = rowNames,
                Cols = colNames,
                Matrix = matrix,
                Unit = request.Metric.Normalize == NormalizeMode.percent ? "%" : "人"
            };
        }

        // ===== CHOROPLETH =====
        public async Task<List<ChoroplethFeatureDto>> GetChoroplethAsync(ChoroplethRequestDto request, CancellationToken ct = default)
        {
            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            var grouped = await baseQ.GroupBy(s => s.CityName)
                                     .Select(g => new { City = g.Key, Val = g.Sum(x => x.Count) })
                                     .ToListAsync(ct);

            double grand = grouped.Sum(x => (double)x.Val);
            return grouped.Select(x => new ChoroplethFeatureDto
            {
                City = x.City,
                Value = x.Val,
                Percentage = grand > 0 ? x.Val / grand : (double?)null,
                Adcode = null
            }).ToList();
        }

        // ===== SCATTER =====
        public async Task<ScatterResultDto> GetScatterAsync(ScatterRequestDto request, CancellationToken ct = default)
        {
            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            var rows = await baseQ.Select(s => new Row
            {
                Year = s.Year,
                City = s.CityName,
                Nat = s.Nationality,
                Ind = s.CategoryNameZh,
                Val = s.Count
            }).ToListAsync(ct);

            var byCity = rows.GroupBy(r => r.City).ToDictionary(g => g.Key, g => g.ToList());

            var points = new List<ScatterPointDto>();
            foreach (var (city, list) in byCity)
            {
                double x = list.Where(r => r.Ind == request.XIndustry).Sum(r => (double)r.Val);
                double y = list.Where(r => r.Ind == request.YIndustry).Sum(r => (double)r.Val);
                double size = list.Sum(r => (double)r.Val);
                points.Add(new ScatterPointDto { City = city, X = x, Y = y, Size = size });
            }
            return new ScatterResultDto { Points = points };
        }

        // ===== HIERARCHY =====
        public async Task<HierarchyResultDto> GetHierarchyAsync(HierarchyRequestDto request, CancellationToken ct = default)
        {
            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            var raw = await baseQ.Select(s => new
            {
                year = s.Year,
                city = s.CityName,
                nationality = s.Nationality,
                industry = s.CategoryNameZh,
                val = s.Count
            }).ToListAsync(ct);

            var list = raw.Select(x => new
            {
                year = x.year,
                city = x.city,
                nationality = DisplayNationality(x.nationality),
                industry = x.industry,
                val = x.val
            }).ToList();

            var first = request.Hierarchy.FirstOrDefault();
            Func<dynamic, string> l1Sel = KeySel(first);
            var level1 = list.GroupBy(l1Sel);

            if (request.Top != null && request.Top.TryGetValue(first.ToString(), out var top) && top > 0)
            {
                level1 = level1.OrderByDescending(g => g.Sum(x => (int)x.val)).Take(top);
            }

            var nodes = new List<HierarchyNodeDto>();
            foreach (var g1 in level1)
            {
                var node = new HierarchyNodeDto { Name = g1.Key, Value = g1.Sum(x => (int)x.val) };
                var remain = request.Hierarchy.Skip(1).ToList();
                if (remain.Count > 0) node.Children = BuildChildren(g1, remain);
                nodes.Add(node);
            }
            return new HierarchyResultDto { Nodes = nodes };
        }

        public async Task<PanelResultDto> GetSeriesPanelsAsync(PanelRequestDto request, CancellationToken ct = default)
        {
            if (!string.Equals(request.PanelBy, "city", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("panelBy 目前僅支援 'city'。");

            var baseQ = ApplyFilters(_db.SexualAssaultStats.AsNoTracking(), request.Filters)
                        .Where(s => s.CategoryType == CrossCategoryType.Industry && !s.IsTotalRow);

            // 先把需要的欄位一次讀回，再在記憶體分組
            var rows = await baseQ.Select(s => new Row
            {
                Year = s.Year,
                City = s.CityName,
                Nat = s.Nationality,
                Ind = s.CategoryNameZh,
                Val = s.Count
            }).ToListAsync(ct);

            // 年份類別：若 Request 有 range，就用 range；否則用資料 distinct
            List<int> yearCatsInt;
            if (request.Filters?.Years?.Mode?.Equals("range", StringComparison.OrdinalIgnoreCase) == true
                && request.Filters.Years.From.HasValue && request.Filters.Years.To.HasValue)
            {
                yearCatsInt = Enumerable.Range(request.Filters.Years.From.Value, request.Filters.Years.To.Value - request.Filters.Years.From.Value + 1).ToList();
            }
            else if (request.Filters?.Years?.Mode?.Equals("multi", StringComparison.OrdinalIgnoreCase) == true
                     && request.Filters.Years.Values is { Count: > 0 })
            {
                yearCatsInt = request.Filters.Years.Values!.Distinct().OrderBy(y => y).ToList();
            }
            else
            {
                yearCatsInt = rows.Select(r => r.Year).Distinct().OrderBy(y => y).ToList();
            }
            var categories = yearCatsInt.Select(y => y.ToString()).ToList();

            // 以城市分群
            var byCity = rows.GroupBy(r => r.City).OrderBy(g => g.Key).ToList();

            // 建立每個城市的面板
            var panels = new List<PanelDto>(byCity.Count);
            foreach (var g in byCity)
            {
                var city = g.Key;
                var list = g.ToList();

                // 行業清單 & 合計
                var industryTotals = list.GroupBy(x => x.Ind)
                                         .Select(x => new { Ind = x.Key, Sum = x.Sum(v => v.Val) })
                                         .OrderByDescending(x => x.Sum)
                                         .ToList();

                var chosen = industryTotals.Select(x => x.Ind).ToList();
                bool addOther = false;

                if (request.SeriesLimit?.TopN is int top && top > 0 && chosen.Count > top)
                {
                    chosen = industryTotals.Take(top).Select(x => x.Ind).ToList();
                    addOther = request.SeriesLimit.OtherBucket;
                }

                // 每個行業 → 年份列
                var series = new List<SeriesLineDto>();
                foreach (var ind in chosen)
                {
                    var yearly = list.Where(r => r.Ind == ind)
                                     .GroupBy(r => r.Year)
                                     .ToDictionary(x => x.Key, x => (double)x.Sum(v => v.Val));

                    var data = yearCatsInt.Select(y => yearly.TryGetValue(y, out var v) ? v : 0.0).ToList();
                    series.Add(new SeriesLineDto { Name = ind, Data = data });
                }

                // 其他桶（若有）
                if (addOther)
                {
                    var chosenSet = chosen.ToHashSet();
                    var otherYearly = list.Where(r => !chosenSet.Contains(r.Ind))
                                          .GroupBy(r => r.Year)
                                          .ToDictionary(x => x.Key, x => (double)x.Sum(v => v.Val));
                    var otherData = yearCatsInt.Select(y => otherYearly.TryGetValue(y, out var v) ? v : 0.0).ToList();
                    series.Add(new SeriesLineDto { Name = "其他", Data = otherData, IsOther = true });
                }

                // 百分比（以城市面板的 grand total 為分母）
                if (request.Metric.Normalize == NormalizeMode.percent)
                {
                    double grand = series.SelectMany(s => s.Data).Sum();
                    if (grand > 0)
                    {
                        foreach (var s in series) s.Data = s.Data.Select(v => v / grand).ToList();
                    }
                }

                panels.Add(new PanelDto
                {
                    Key = city,
                    Title = $"{city} {yearCatsInt.First()}–{yearCatsInt.Last()} 各行業",
                    Categories = categories,
                    Series = series
                });
            }

            return new PanelResultDto
            {
                Meta = new
                {
                    panelBy = "city",
                    x = request.View.X.ToString(),
                    series = request.View.Series?.ToString(),
                    stack = request.View.Stack,
                    years = yearCatsInt,
                    unit = request.Metric.Normalize == NormalizeMode.percent ? "%" : "人"
                },
                Panels = panels
            };
        }

        // ===== helpers =====
        private static List<HierarchyNodeDto> BuildChildren(IEnumerable<dynamic> parent, List<DimensionKey> dims)
        {
            if (dims.Count == 0) return null!;
            Func<dynamic, string> sel = KeySel(dims.First());
            var groups = parent.GroupBy(sel).ToList();
            var list = new List<HierarchyNodeDto>();
            foreach (var g in groups)
            {
                var node = new HierarchyNodeDto { Name = g.Key, Value = g.Sum(x => (int)x.val) };
                if (dims.Count > 1) node.Children = BuildChildren(g, dims.Skip(1).ToList());
                list.Add(node);
            }
            return list;
        }

        private static Func<dynamic, string> KeySel(DimensionKey dim) => dim switch
        {
            DimensionKey.year => (d => ((int)d.year).ToString()),
            DimensionKey.city => (d => (string)d.city),
            DimensionKey.nationality => (d => (string)d.nationality),
            DimensionKey.industry => (d => (string)d.industry),
            _ => (d => "")
        };

        private static IQueryable<SexualAssaultStat> ApplyFilters(IQueryable<SexualAssaultStat> q, FiltersDto f)
        {
            if (f.Years != null)
            {
                if (string.Equals(f.Years.Mode, "single", StringComparison.OrdinalIgnoreCase) && f.Years.Value.HasValue)
                    q = q.Where(s => s.Year == f.Years.Value.Value);
                else if (string.Equals(f.Years.Mode, "range", StringComparison.OrdinalIgnoreCase) && f.Years.From.HasValue && f.Years.To.HasValue)
                    q = q.Where(s => s.Year >= f.Years.From!.Value && s.Year <= f.Years.To!.Value);
                else if (string.Equals(f.Years.Mode, "multi", StringComparison.OrdinalIgnoreCase) && f.Years.Values is { Count: > 0 })
                    q = q.Where(s => f.Years.Values!.Contains(s.Year));
            }

            if (f.Cities is { Count: > 0 }) q = q.Where(s => f.Cities!.Contains(s.CityName));

            if (f.Nationalities is { Count: > 0 })
            {
                var codes = f.Nationalities.Select(ParseNationality).ToList();
                q = q.Where(s => codes.Contains(s.Nationality));
            }

            if (f.Industries is { Count: > 0 }) q = q.Where(s => f.Industries!.Contains(s.CategoryNameZh));
            return q;
        }

        private static NationalityCode ParseNationality(string name)
        {
            var n = name.Replace("外國籍-", "").Replace("外國籍－", "").Trim();

            string target = n switch
            {
                "印尼" => "Indonesia",
                "泰國" => "Thailand",
                "越南" => "Vietnam",
                "菲律賓" => "Philippines",
                "馬來西亞" => "Malaysia",
                "外國籍" => "Foreign",
                "其他" => "Other",
                _ => n
            };

            var names = Enum.GetNames(typeof(NationalityCode));
            if (names.Contains(target)) return (NationalityCode)Enum.Parse(typeof(NationalityCode), target, true);
            if (names.Contains("Unknown")) return (NationalityCode)Enum.Parse(typeof(NationalityCode), "Unknown", true);

            var vals = (NationalityCode[])Enum.GetValues(typeof(NationalityCode));
            return vals.Length > 0 ? vals[0] : default;
        }

        private static string DisplayNationality(NationalityCode code)
        {
            var name = code.ToString();
            return name switch
            {
                "Indonesia" => "外國籍-印尼",
                "Thailand" => "外國籍-泰國",
                "Vietnam" => "外國籍-越南",
                "Philippines" => "外國籍-菲律賓",
                "Malaysia" => "外國籍－馬來西亞",
                "Foreign" => "外國籍",
                "Other" => "外國籍-其他",
                _ => name
            };
        }

        private static List<double> MovingAverage(List<double> values, int k)
        {
            if (k <= 1 || values.Count == 0) return values;
            var res = new List<double>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                int start = Math.Max(0, i - k + 1);
                int len = i - start + 1;
                double avg = values.Skip(start).Take(len).Average();
                res.Add(avg);
            }
            return res;
        }

        private sealed class Row
        {
            public int Year { get; set; }
            public string City { get; set; } = default!;
            public NationalityCode Nat { get; set; }
            public string Ind { get; set; } = default!;
            public int Val { get; set; }
        }
    }
}

