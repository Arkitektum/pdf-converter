using PuppeteerSharp;

namespace PdfGenerator.Helpers
{
    public class ImageHelper
    {
        public static readonly string[] SupportedImageMimeTypes = new[] {
            "image/apng",
            "image/avif",
            "image/gif",
            "image/jpeg", 
            "image/png", 
            "image/svg+xml",
            "image/webp",
        };

        public static bool IsImage(string mimeType) => mimeType != null && SupportedImageMimeTypes.Contains(mimeType);

        public static async Task SetImageSizeAsync(Page page, PdfOptions pdfOptions)
        {
            var width = pdfOptions.Width ?? $"{pdfOptions.Format.Width}in";
            var height = pdfOptions.Height ?? $"{pdfOptions.Format.Height}in";

            var cssContent = $@"
                img {{
                    width: calc({(pdfOptions.Landscape ? height : width)} - {pdfOptions.MarginOptions.Left} - {pdfOptions.MarginOptions.Right});
                    height: calc({(pdfOptions.Landscape ? width : height)} - {pdfOptions.MarginOptions.Top} - {pdfOptions.MarginOptions.Bottom});
                }}
            ";

            await page.AddStyleTagAsync(new AddTagOptions { Content = cssContent });
        }

        public static async Task<bool> IsLandscapeAsync(Page page)
        {
            var script = @"
                () => {
                    const image = document.querySelector('img');
                    return [image.clientWidth, image.clientHeight];
                }
            ";

            var size = await page.EvaluateFunctionAsync<int[]>(script);

            return size[0] > size[1];
        }
    }
}
