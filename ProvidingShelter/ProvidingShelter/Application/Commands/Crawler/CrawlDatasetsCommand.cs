namespace ProvidingShelter.Application.Commands.Crawler;

public sealed record CrawlDatasetsCommand(
    int Page = 1,
    int Size = 10,
    string Sort = "_score_desc",
    string KeywordUrlEncoded = "%E6%80%A7%E4%BE%B5", // 預設「性侵」
    bool DownloadFiles = false);
