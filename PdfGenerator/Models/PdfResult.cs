namespace PdfGenerator.Models
{
    public class PdfResult
    {
        public static string ContentType => "application/pdf";
        public Stream Data { get; set; }
        public string FileName { get; set; }
        public long FileSize => Data?.Length ?? 0;
    }
}
