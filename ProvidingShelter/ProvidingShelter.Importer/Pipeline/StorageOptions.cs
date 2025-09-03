namespace ProvidingShelter.Importer.Pipeline
{
    public class StorageOptions
    {
        public string RootPath { get; set; } = @"C:\Users\timchen\Documents\GovOpenData";
        public int ContentInDbMaxBytes { get; set; } = 1_048_576; // 1MB
    }

    public class FormatOptions
    {
        public string[] Allow { get; set; } = new[] { "CSV", "JSON", "XML", "XLS", "XLSX", "ODS", "API", "WEBSERVICES", "GEOJSON", "RSS", "CAP", "TXT" };
        public string[] Container { get; set; } = new[] { "ZIP", "RAR", "7Z", "TAR", "壓縮檔" };
        public string[] Deny { get; set; } = new[] { "PDF", "DOC", "DOCX", "JPG", "PNG", "WMS", "其他", "ODT" };
        public GeoOptions Geo { get; set; } = new();
    }
    public class GeoOptions { public bool UseGdal { get; set; } = false; }
}
