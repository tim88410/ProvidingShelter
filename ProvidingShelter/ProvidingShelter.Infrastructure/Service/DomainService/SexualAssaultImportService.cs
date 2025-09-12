using System.Text.RegularExpressions;
using ProvidingShelter.Domain.Entities;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Models;
using ProvidingShelter.Infrastructure.Service.ExternalService;

namespace ProvidingShelter.Infrastructure.Service.DomainService
{
    public interface ISexualAssaultImportService
    {
        Task<int> ImportFromOdsUrlsAsync(IEnumerable<SexualAssaultOdsDetail> datails, CancellationToken ct = default);
    }

    public class SexualAssaultImportService : ISexualAssaultImportService
    {
        private readonly IOdsDownloader _downloader;
        private readonly IOdsReader _reader;
        private readonly ISexualAssaultInformationRepository _repo;

        public SexualAssaultImportService(IOdsDownloader downloader, IOdsReader reader, ISexualAssaultInformationRepository repo)
        {
            _downloader = downloader;
            _reader = reader;
            _repo = repo;
        }

        public async Task<int> ImportFromOdsUrlsAsync(IEnumerable<SexualAssaultOdsDetail> datails, CancellationToken ct = default)
        {
            int total = 0;

            foreach (var datail in datails.Distinct())
            {
                using var stream = await _downloader.DownloadAsync(datail.DownloadURL, ct);
                var rows = await _reader.ReadAsync(stream, ct);

                var fileYear = ExtractGregorianYearFromName(datail.Title) ?? ExtractGregorianYearFromName(datail.DownloadURL);
                var list = new List<SexualAssaultInformation>(rows.Count);

                foreach (var r in rows)
                {
                    // 1) 先決定 InfoTimeYear（若空就用檔名推）
                    short? infoYear = GetShortOrNull(r, "INFOTIMEYEAR");
                    if (infoYear is null && fileYear is not null)
                        infoYear = (short)fileYear.Value;

                    byte? infoMonth = GetByteOrNull(r, "INFOTIMEMONTH");

                    // 2) LastOccurTime：9碼矯正
                    var lastOccurDt = NormalizeLastOccurTime9(GetStr(r, "LASTOCCURTIME"));

                    // 3) ReceiveTime：優先 InfoTimeYear+InfoTimeMonth，否則 RECEIVETIME
                    DateTime? receiveTime = null;
                    if (infoYear.HasValue && infoMonth.HasValue && infoMonth.Value >= 1 && infoMonth.Value <= 12)
                    {
                        receiveTime = new DateTime(infoYear.Value, infoMonth.Value, 1, 0, 0, 0);
                    }
                    else
                    {
                        receiveTime = ParseReceiveTime(GetStr(r, "RECEIVETIME"));
                    }

                    var relRaw = GetStr(r, "RELATION");
                    var rel = (!string.IsNullOrWhiteSpace(relRaw) && !Regex.IsMatch(relRaw, @"^\d+$"))
                        ? relRaw.ToUpperInvariant()
                        : relRaw;

                    var item = new SexualAssaultInformation
                    {
                        OwnerCityCode = GetStr(r, "OWNERCITYCODE"),
                        OccurCity = GetStr(r, "OCCURCITY"),
                        OccurTown = GetStr(r, "OCCURTOWN"),
                        TownCode = GetStr(r, "TOWNCODE"),
                        InfoerType = GetStr(r, "INFOERTYPE"),
                        InfoUnit = GetStr(r, "INFOUNIT"),
                        OtherInfoerType = GetStr(r, "OTHERINFOERTYPE"),
                        OtherInfoUnit = GetStr(r, "OTHERINFOUNIT"),
                        ClientId = GetStr(r, "CLIENTID"),
                        Gender = FirstNonEmpty(GetStr(r, "GENDER"), GetStr(r, "SEXID")),
                        BDate = GetIntOrNull(r, "BDATE"),
                        IdType = GetStr(r, "IDTYPE"),
                        Occupation = GetStr(r, "OCCUPATION"),
                        OtherOccupation = GetStr(r, "OTHEROCCUPATION"),
                        Education = GetStr(r, "EDUCATION"),
                        Maimed = GetStr(r, "MAIMED"),
                        OtherMaimed = GetStr(r, "OTHERMAIMED"),
                        OtherMaimed2 = GetStr(r, "OTHERMAIMED2"),
                        School = GetStr(r, "SCHOOL"),
                        DId = GetStr(r, "DID"),
                        DSexId = GetStr(r, "DSEXID"),
                        DBDate = GetIntOrNull(r, "DBDATE"),
                        NumOfSuspect = GetByteOrNull(r, "NUMOFSUSPECT"),
                        Relation = rel,
                        OtherRelation = GetStr(r, "OTHERRELATION"),
                        OccurPlace = GetStr(r, "OCCURPLACE"),
                        OtherOccurPlace = GetStr(r, "OTHEROCCURPLACE"),

                        LastOccurTime = lastOccurDt,
                        InfoTimeYear = infoYear,
                        InfoTimeMonth = infoMonth,

                        ReceiveTime = receiveTime,
                        NotifyDate = (lastOccurDt.HasValue && receiveTime.HasValue)
                                            ? (int?)(receiveTime.Value.Date - lastOccurDt.Value.Date).TotalDays
                                            : null
                    };

                    list.Add(item);
                }

                if (list.Count > 0)
                {
                    await _repo.AddRangeAsync(list, ct);
                    total += list.Count;
                }
            }

            return total;
        }

        private static string? GetStr(Dictionary<string, string> row, string key)
            => row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

        private static int? GetIntOrNull(Dictionary<string, string> row, string key)
            => row.TryGetValue(key, out var v) && int.TryParse(v.Trim(), out var n) ? n : null;

        private static short? GetShortOrNull(Dictionary<string, string> row, string key)
            => row.TryGetValue(key, out var v) && short.TryParse(v.Trim(), out var n) ? n : null;

        private static byte? GetByteOrNull(Dictionary<string, string> row, string key)
            => row.TryGetValue(key, out var v) && byte.TryParse(v.Trim(), out var n) ? n : null;

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        // 9 碼 LastOccurTime 矯正：
        // - "000000000" → null
        // - 其它：YYY(民國)+MM+DD+HH → 西元年 + 分秒 = 00
        // - 若 MM/DD/HH 全 0（如 096000000）→ 西元年-01-01 00:00:00
        private static DateTime? NormalizeLastOccurTime9(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = new string(raw.Where(char.IsDigit).ToArray());
            if (s.Length != 9) return null;
            if (s == "000000000") return null;

            int roc = SafeParse(s[..3]);
            int year = 1911 + roc;
            int mm = SafeParse(s.Substring(3, 2));
            int dd = SafeParse(s.Substring(5, 2));
            int hh = SafeParse(s.Substring(7, 2));

            if (mm == 0 && dd == 0 && hh == 0)
            {
                mm = 1; dd = 1; hh = 0;
            }
            else
            {
                if (mm <= 0) mm = 1;
                if (dd <= 0) dd = 1;
                if (hh < 0) hh = 0;
            }

            try
            {
                // 分秒固定 00
                return new DateTime(year, mm, Math.Min(dd, DateTime.DaysInMonth(year, mm)), hh, 0, 0);
            }
            catch { return null; }
        }

        // RECEIVETIME: YYYYMMDDHHMI（或至少 YYYYMMDD）
        private static DateTime? ParseReceiveTime(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = new string(raw.Where(char.IsDigit).ToArray());
            try
            {
                if (s.Length >= 12)
                {
                    int y = SafeParse(s[..4]);
                    int m = SafeParse(s.Substring(4, 2));
                    int d = SafeParse(s.Substring(6, 2));
                    int hh = SafeParse(s.Substring(8, 2));
                    int mi = SafeParse(s.Substring(10, 2));
                    m = Math.Clamp(m, 1, 12);
                    d = Math.Clamp(d, 1, DateTime.DaysInMonth(y, m));
                    hh = Math.Clamp(hh, 0, 23);
                    mi = Math.Clamp(mi, 0, 59);
                    return new DateTime(y, m, d, hh, mi, 0);
                }
                else if (s.Length == 8)
                {
                    int y = SafeParse(s[..4]);
                    int m = SafeParse(s.Substring(4, 2));
                    int d = SafeParse(s.Substring(6, 2));
                    m = Math.Clamp(m, 1, 12);
                    d = Math.Clamp(d, 1, DateTime.DaysInMonth(y, m));
                    return new DateTime(y, m, d, 0, 0, 0);
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private static int SafeParse(string s) => int.TryParse(s, out var n) ? n : 0;

        // 檔名/URL 取民國年 → 西元
        private static int? ExtractGregorianYearFromName(string? nameOrUrl)
        {
            if (string.IsNullOrWhiteSpace(nameOrUrl)) return null;
            var raw = Uri.UnescapeDataString(nameOrUrl);
            try
            {
                if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
                    raw = uri.Segments.LastOrDefault() ?? uri.AbsolutePath;
            }
            catch { /* ignore */ }

            var fname = Path.GetFileNameWithoutExtension(raw);

            var m = Regex.Match(fname, @"\b(1\d{2})\d{0,4}\b"); // 1030101 / 1100101 / 103-...
            if (m.Success) return 1911 + int.Parse(m.Groups[1].Value);

            m = Regex.Match(fname, @"\b([89]\d)\d{0,4}\b");     // 80、99、800101...
            if (m.Success) return 1911 + int.Parse(m.Groups[1].Value);

            m = Regex.Match(fname, @"民國\s*(\d{2,3})");         // 民國110年
            if (m.Success) return 1911 + int.Parse(m.Groups[1].Value);

            m = Regex.Match(fname, @"\b(20\d{2}|19\d{2})\b");    // 已是西元
            if (m.Success) return int.Parse(m.Value);

            return null;
        }
    }
}
