namespace PdfGenerator.Models
{
    public class PdfGenerationOptions
    {
        public string HtmlData { get; set; }
        public string FileName { get; set; }
        public PaperOptions Paper { get; set; } = new PaperOptions();

        public class PaperOptions
        {
            public string PaperWidth { get; set; }
            public string PaperHeight { get; set; }
            public string MarginTop { get; set; }
            public string MarginRight { get; set; }
            public string MarginBottom { get; set; }
            public string MarginLeft { get; set; }
        }
    }
}
