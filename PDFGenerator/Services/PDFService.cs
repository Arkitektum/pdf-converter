using Ionic.Zip;
using Microsoft.Extensions.Options;
using PdfGenerator.Exceptions;
using PdfGenerator.Helpers;
using PdfGenerator.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text;
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

                var pdfOptions = GetPdfOptions(options);
                var pdfStream = await page.PdfStreamAsync(pdfOptions);
                var fileName = await CreateFileNameAsync(page);

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

        public async Task<PdfResult> GeneratePdfFromFileAsync(IFormFile file, PdfOptions options)
        {
            try
            {
                var startTime = DateTime.Now;
                var content = await GenerateContentAsync(file);
                var browser = await _browserProvider.GetBrowserAsync();

                using var page = await browser.NewPageAsync();              
                await page.SetContentAsync(content);

                var pdfOptions = GetPdfOptions(options);

                if (ImageHelpers.IsImage(file))
                {
                    pdfOptions.Landscape = await ImageHelpers.IsLandscapeAsync(page);
                    await ImageHelpers.SetImageSizeAsync(page, pdfOptions);
                }

                var pdfStream = await page.PdfStreamAsync(pdfOptions);
                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
                var logEntry = $@"Genererte PDF ""{fileName}"" ({Math.Round(pdfStream.Length / 1024f / 1024f, 2)} MB) av filen ""{file.FileName}"" på {Math.Round(DateTime.Now.Subtract(startTime).TotalSeconds, 2)} sek.";

                _logger.LogInformation(logEntry);

                return new PdfResult
                {
                    FileName = fileName,
                    Data = pdfStream,
                    Log = logEntry
                };
            }
            catch (Exception exception)
            {
                return new PdfResult
                {
                    Log = $@"Kunne ikke generere PDF av filen ""{file.FileName}"": {exception.Message}"
                };
            }
        }

        public async Task<MemoryStream> GeneratePdfZipFromFilesAsync(List<IFormFile> files, PdfOptions options)
        {
            var pdfTaskList = files.Select(file => GeneratePdfFromFileAsync(file, options));

            await Task.WhenAll(pdfTaskList);
            
            using var archive = new ZipFile();
            var log = new List<string>();

            foreach (var pdfTask in pdfTaskList)
            {
                var pdfResult = await pdfTask;

                if (pdfResult.Data != null)
                    archive.AddEntry(pdfResult.FileName, pdfResult.Data);

                log.Add(pdfResult.Log);
            }

            archive.AddEntry("log.txt", await CreateLogStreamAsync(log));

            var memoryStream = new MemoryStream();
            archive.Save(memoryStream);

            return memoryStream;
        }

        private PuppeteerPdfOptions GetPdfOptions(PdfOptions options)
        {
            return new()
            {
                Width = options?.PaperWidth ?? _config.PaperWidth,
                Height = options?.PaperHeight ?? _config.PaperHeight,
                MarginOptions = new MarginOptions
                {
                    Top = options?.MarginTop ?? _config.MarginTop,
                    Right = options?.MarginRight ?? _config.MarginRight,
                    Bottom = options?.MarginBottom ?? _config.MarginBottom,
                    Left = options?.MarginLeft ?? _config.MarginLeft
                },
                PrintBackground = options?.PrintBackground ?? _config.PrintBackground
            };
        }

        private static async Task<string> GenerateContentAsync(IFormFile file)
        {
            if (file.ContentType == null)
                throw new ContentTypeException("Filen mangler Content-Type.");

            if (file.ContentType == "text/html")
                return await TemplatingHelper.StreamToStringAsync(file.OpenReadStream());
            else if (ImageHelpers.IsImage(file))
                return await CreateImageHtmlAsync(file);

            throw new ContentTypeException(@$"Filen har ugyldig Content-Type ""{file.ContentType}"".");
        }

        private static async Task<string> CreateImageHtmlAsync(IFormFile file)
        {
            var template = await TemplatingHelper.GetTemplateAsync("image.html");

            var parameters = new Dictionary<string, string>()
            {
                { "mimeType", file.ContentType },
                { "imageData", await ImageHelpers.ConvertImageToBase64StringAsync(file) }
            };

            return TemplatingHelper.FormatHtml(template, parameters);
        }

        private static async Task<Stream> CreateLogStreamAsync(IEnumerable<string> log)
        {
            var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

            foreach (var logEntry in log)
                await streamWriter.WriteLineAsync(logEntry);

            await streamWriter.FlushAsync();
            memoryStream.Position = 0;

            return memoryStream;
        }

        private static async Task<string> CreateFileNameAsync(Page page)
        {
            var pageTitle = await page.GetTitleAsync();

            if (string.IsNullOrWhiteSpace(pageTitle))
                return $"pdf-document-{DateTime.Now:yyyyMMddTHHmmss}.pdf";
            
            foreach (var c in Path.GetInvalidFileNameChars())
                pageTitle = pageTitle.Replace(c, '_');

            return $"{pageTitle}.pdf";
        }
    }
}
