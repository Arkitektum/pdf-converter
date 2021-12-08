using Microsoft.AspNetCore.Http;
using PuppeteerSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PdfGenerator.Helpers
{
    public class ImageHelpers
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

        public static bool IsImage(IFormFile file) => SupportedImageMimeTypes.Contains(file.ContentType);

        public static async Task<string> ConvertImageToBase64StringAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static async Task SetImageSizeAsync(Page page, PdfOptions pdfOptions)
        {
            var cssContent = $@"
                img {{
                    width: calc({(pdfOptions.Landscape ? pdfOptions.Height : pdfOptions.Width)} - {pdfOptions.MarginOptions.Left} - {pdfOptions.MarginOptions.Right});
                    height: calc({(pdfOptions.Landscape ? pdfOptions.Width : pdfOptions.Height)} - {pdfOptions.MarginOptions.Top} - {pdfOptions.MarginOptions.Bottom});
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
