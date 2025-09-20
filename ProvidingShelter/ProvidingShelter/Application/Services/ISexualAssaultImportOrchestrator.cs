namespace ProvidingShelter.Application.Services
{
    public interface ISexualAssaultImportOrchestrator
    {
        /// <summary>
        /// 上傳「性侵害案件被害人年齡與國籍別交叉統計」ODS，轉檔並解析後入庫，回傳 ImportId。
        /// 若檔案與既有匯入（以 SHA256）重複，直接回傳既有 ImportId（不重覆寫入）。
        /// </summary>
        Task<Guid> UploadAndImportAgeByNationalityAsync(IFormFile file, CancellationToken ct);
    }
}
