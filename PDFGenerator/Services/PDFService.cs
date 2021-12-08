using Ionic.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfGenerator.Helpers;
using PdfGenerator.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<PdfFile> GeneratePdfFromHtmlStringAsync(string htmlString, PdfCustomOptions options)
        {
            try
            {
                byte[] pdfData;
                var start = DateTime.Now;
                var fileName = $"{Guid.NewGuid()}.pdf";
                var browser = await _browserProvider.GetBrowserAsync();

                using (var page = await browser.NewPageAsync())
                {
                    await page.SetContentAsync(htmlString);

                    var pdfOptions = GetPdfOptions(options);

                    pdfData = await page.PdfDataAsync(pdfOptions);
                }

                _logger.LogInformation($"Genererte PDF {fileName} ({Math.Round(pdfData.Length / 1024f, 2)} KB) på {Math.Round(DateTime.Now.Subtract(start).TotalSeconds, 2)} sek.");

                return new PdfFile
                {
                    Data = pdfData,
                    FileSize = pdfData.Length,
                    FileName = fileName
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Puppeteer kunne ikke generere PDF!");
                throw;
            }
        }

        public async Task<MemoryStream> GeneratePdfArchiveFromFilesAsync(List<IFormFile> files, PdfCustomOptions options)
        {
            var taskList = new List<(string FileName, Task<Stream> PdfTask)>();

            foreach (var file in files)
            {
                var task = GeneratePdfStreamAsync(file, options);

                if (task != null)
                    taskList.Add((file.FileName, task));
            }

            await Task.WhenAll(taskList.Select(task => task.PdfTask));

            using var archive = new ZipFile();

            foreach (var (FileName, PdfTask) in taskList)
            {
                var stream = await PdfTask;
                archive.AddEntry($"{Path.GetFileNameWithoutExtension(FileName)}.pdf", stream);
            }

            var memoryStream = new MemoryStream();

            archive.Save(memoryStream);

            return memoryStream;
        }

        private async Task<Stream> GeneratePdfStreamAsync(IFormFile file, PdfCustomOptions options)
        {
            var content = await GenerateContentAsync(file);

            if (content == null)
                return null;

            var start = DateTime.Now;
            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
            var browser = await _browserProvider.GetBrowserAsync();

            using var page = await browser.NewPageAsync();
            await page.SetContentAsync(content);

            var pdfOptions = GetPdfOptions(options);

            if (ImageHelpers.IsImage(file))
            {
                pdfOptions.Landscape = await ImageHelpers.IsLandscapeAsync(page);
                await ImageHelpers.SetImageSizeAsync(page, pdfOptions);
            }

            var stream = await page.PdfStreamAsync(pdfOptions);

            _logger.LogInformation($"Genererte PDF {fileName} ({Math.Round(stream.Length / 1024f, 2)} KB) på {Math.Round(DateTime.Now.Subtract(start).TotalSeconds, 2)} sek.");

            return stream;
        }

        private PdfOptions GetPdfOptions(PdfCustomOptions options)
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
                return null;

            if (file.ContentType == "text/html")
                return await TemplatingHelper.StreamToStringAsync(file.OpenReadStream());
            else if (ImageHelpers.IsImage(file))
                return await CreateImageHtmlAsync(file);

            return null;
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
    }
}
