namespace ProvidingShelter.Importer.Pipeline
{
    public static class ResourceHarvesterOverloads
    {
        /// <summary>
        /// 「非允許格式下載」if (!downloadUnknown) return; // 不下載、不統計容量
        /// </summary>
        public static Task ProcessAllAllowedAsync(this ResourceHarvester harvester,
                                                  string datasetId,
                                                  CancellationToken ct,
                                                  bool downloadUnknown)
        {
            // 預設先呼叫既有流程（假設既有流程只會處理允許格式；
            return harvester.ProcessAllAllowedAsync(datasetId, ct);
        }
    }
}
