using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfGenerator.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Threading.Tasks;

namespace PdfGenerator.Services
{
    public class PdfService : IPdfService
    {
        private readonly IBrowserProvider _browserProvider;
        private readonly PdfServiceConfig _config;
        private readonly ILogger<PdfService> _logger;

        public PdfService(
            IBrowserProvider browserProvider,
            IOptions<PdfServiceConfig> options,
            ILogger<PdfService> logger)
        {
            _browserProvider = browserProvider;
            _config = options.Value;
            _logger = logger;
        }

        public async Task<byte[]> GeneratePdfAsync(PdfGenerationOptions options)
        {
            try
            {
                byte[] pdfData = null;

                var browser = await _browserProvider.GetBrowser();

                using (var page = await browser.NewPageAsync())
                {
                    await page.SetContentAsync(options.HtmlData);

                    var pdfOptions = new PdfOptions
                    {
                        Width = options.Paper.PaperWidth ?? _config.PaperWidth,
                        Height = options.Paper.PaperHeight ?? _config.PaperHeight,
                        MarginOptions = new MarginOptions
                        {
                            Top = options.Paper.MarginTop ?? _config.MarginTop,
                            Right = options.Paper.MarginRight ?? _config.MarginRight,
                            Bottom = options.Paper.MarginBottom ?? _config.MarginBottom,
                            Left = options.Paper.MarginLeft ?? _config.MarginLeft
                        },
                        PrintBackground = true
                    };

                    pdfData = await page.PdfDataAsync(pdfOptions);
                }

                return pdfData;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Puppeteer kunne ikke generere PDF!");
                throw;
            }
        }
    }
}
