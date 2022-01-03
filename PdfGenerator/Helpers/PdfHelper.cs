using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PdfGenerator.Helpers
{
    public class PdfHelper
    {
        public static async Task<string> CreateFileNameAsync(Page page)
        {
            var pageTitle = await page.GetTitleAsync();

            if (string.IsNullOrWhiteSpace(pageTitle))
                return $"pdf-document-{DateTime.Now:yyyyMMddTHHmmss}.pdf";

            foreach (var c in Path.GetInvalidFileNameChars())
                pageTitle = pageTitle.Replace(c, '_');

            return $"{pageTitle}.pdf";
        }

        public static PaperFormat ParsePaperFormat(string format)
        {
            return format switch
            {
                "A0" => PaperFormat.A0,
                "A1" => PaperFormat.A1,
                "A2" => PaperFormat.A2,
                "A3" => PaperFormat.A3,
                "A4" => PaperFormat.A4,
                "A5" => PaperFormat.A5,
                "A6" => PaperFormat.A6,
                _ => PaperFormat.A4,
            };
        }
    }
}
