using Microsoft.Extensions.Options;
using PdfGenerator.Helpers;
using PdfGenerator.Models;
using PuppeteerSharp.Media;
using PdfOptions = PdfGenerator.Models.PdfOptions;
using PuppeteerPdfOptions = PuppeteerSharp.PdfOptions;

namespace PdfGenerator.Services
{
    public class PdfService : IPdfService
    {
        private readonly IBrowserProvider _browserProvider;
        private readonly PdfSettings _config;
        private readonly ILogger<PdfService> _logger;

        public PdfService(
            IBrowserProvider browserProvider,
            IOptions<PdfSettings> options,
            ILogger<PdfService> logger)
        {
            _browserProvider = browserProvider;
            _config = options.Value;
            _logger = logger;
        }

        public async Task<PdfResult> GeneratePdfFromHtmlStringAsync(string htmlString, PdfOptions options)
        {
            try
            {
                var start = DateTime.Now;
                var browser = await _browserProvider.GetBrowserAsync();

                using var page = await browser.NewPageAsync();
                await page.SetContentAsync(htmlString);

                var pdfOptions = GetPdfOptionsAsync(options);
                var pdfStream = await page.PdfStreamAsync(pdfOptions);
                var fileName = await PdfHelper.CreateFileNameAsync(page);

                _logger.LogInformation(@$"Genererte PDF ""{fileName}"" ({Math.Round(pdfStream.Length / 1024f / 1024f, 2)} MB) på {Math.Round(DateTime.Now.Subtract(start).TotalSeconds, 2)} sek.");

                return new PdfResult
                {
                    FileName = fileName,
                    Data = pdfStream
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kunne ikke generere PDF fra HTML.");
                throw;
            }
        }

        public async Task<PdfResult> GeneratePdfFromImageAsync(PdfInputData inputData, PdfOptions options)
        {
            try
            {
                var startTime = DateTime.Now;
                var content = await CreateImageHtmlAsync(inputData);
                var browser = await _browserProvider.GetBrowserAsync();

                using var page = await browser.NewPageAsync();              
                await page.SetContentAsync(content);

                var pdfOptions = GetPdfOptionsAsync(options);
                
                pdfOptions.Landscape = await ImageHelper.IsLandscapeAsync(page);
                await ImageHelper.SetImageSizeAsync(page, pdfOptions);

                if (inputData.Title != null)
                    await SetImageHeaderAsync(inputData.Title, pdfOptions);

                var pdfStream = await page.PdfStreamAsync(pdfOptions);
                var fileName = $"{Path.GetFileNameWithoutExtension(inputData.File.FileName)}.pdf";

                _logger.LogInformation($@"Genererte PDF ""{fileName}"" ({Math.Round(pdfStream.Length / 1024f / 1024f, 2)} MB) av filen ""{inputData.File.FileName}"" på {Math.Round(DateTime.Now.Subtract(startTime).TotalSeconds, 2)} sek.");

                return new PdfResult
                {
                    FileName = fileName,
                    Data = pdfStream,
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $@"Kunne ikke generere PDF av filen ""{inputData.File.FileName}""");
                throw;
            }
        }

        private PuppeteerPdfOptions GetPdfOptionsAsync(PdfOptions options)
        {
            var pdfOptions = new PuppeteerPdfOptions
            {  
                MarginOptions = new MarginOptions
                {
                    Top = options?.MarginTop ?? _config.MarginTop,
                    Right = options?.MarginRight ?? _config.MarginRight,
                    Bottom = options?.MarginBottom ?? _config.MarginBottom,
                    Left = options?.MarginLeft ?? _config.MarginLeft
                },
                PrintBackground = options?.PrintBackground ?? _config.PrintBackground
            };

            if (options?.Format != null)
            {
                pdfOptions.Format = PdfHelper.ParsePaperFormat(options.Format);
            }
            else
            {
                pdfOptions.Width = options?.PaperWidth ?? _config.PaperWidth;
                pdfOptions.Height = options?.PaperHeight ?? _config.PaperHeight;
            }

            return pdfOptions;
        }

        private static async Task<string> CreateImageHtmlAsync(PdfInputData inputData)
        {
            var template = await TemplatingHelper.GetTemplateAsync("image.html");

            var parameters = new Dictionary<string, string>()
            {
                { "mimeType", inputData.File.ContentType },
                { "imageData", await ConvertToBase64StringAsync(inputData.File) }
            };

            return TemplatingHelper.FormatHtml(template, parameters);
        }

        private static async Task<string> CreateHeaderHtmlAsync(string title, PuppeteerPdfOptions pdfOptions)
        {
            var template = await TemplatingHelper.GetTemplateAsync("header.html");

            var parameters = new Dictionary<string, string>()
            {
                { "title", title },
                { "marginTop", pdfOptions.MarginOptions.Top },
                { "marginRight", pdfOptions.MarginOptions.Right },
                { "marginLeft", pdfOptions.MarginOptions.Left },
            };

            return TemplatingHelper.FormatHtml(template, parameters);
        }

        private static async Task SetImageHeaderAsync(string title, PuppeteerPdfOptions pdfOptions)
        {
            pdfOptions.DisplayHeaderFooter = true;
            pdfOptions.HeaderTemplate = await CreateHeaderHtmlAsync(title, pdfOptions);
            pdfOptions.FooterTemplate = @"<span></span>";
        }

        private static async Task<string> ConvertToBase64StringAsync(IFormFile file)
        {
            var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);

            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
