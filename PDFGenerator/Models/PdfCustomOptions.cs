namespace PdfGenerator.Models
{
    public class PdfCustomOptions
    {
        public string PaperWidth { get; set; }
        public string PaperHeight { get; set; }
        public string MarginTop { get; set; }
        public string MarginRight { get; set; }
        public string MarginBottom { get; set; }
        public string MarginLeft { get; set; }
        public bool? PrintBackground { get; set; } = true;
    }
}
