namespace ProvidingShelter.Common.AppSettings
{
    public sealed class AnalyticsCacheOptions
    {
        public bool Enabled { get; set; } = true;
        public int AbsoluteExpirationMinutes { get; set; } = 1440; // 預設 1 天
    }
}
