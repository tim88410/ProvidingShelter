namespace ProvidingShelter.Common.AppSettings
{
    public sealed class DataImportSettings
    {
        public SexualAssaultSettings SexualAssault { get; set; } = new();

        public sealed class SexualAssaultSettings
        {
            /// <summary>
            /// 上傳檔案儲存的根目錄，如：C:\Users\timchen\Documents\GovOpenData\SexualAssaultStatistics
            /// </summary>
            public string BasePath { get; set; } = string.Empty;

            public LibreOfficeSettings LibreOffice { get; set; } = new();
        }

        public sealed class LibreOfficeSettings
        {
            /// <summary>
            /// LibreOffice soffice 可執行檔路徑（可填 "soffice" 若已加入 PATH）
            /// </summary>
            public string SofficePath { get; set; } = "soffice";
        }
    }
}