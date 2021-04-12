namespace PdfGenerator.Models
{
    public class PdfFile
    {
        public static string ContentType => "application/pdf";
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
    }
}
