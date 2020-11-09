using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PDFGenerator.Services
{
    public class PDFService : IPDFService
    {
        private readonly PDFServiceConfig _pdfServiceConfig;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<PDFService> _logger;
        private const string WebSocketDebuggerUrlCacheKey = "_WebSocketDebuggerUrl";
        private const int CacheDurationDays = 365;

        public PDFService(
            IOptions<PDFServiceConfig> pdfServiceOptions,
            IMemoryCache memoryCache,
            ILogger<PDFService> logger)
        {
            _pdfServiceConfig = pdfServiceOptions.Value;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<byte[]> GeneratePdfAsync(string htmlData)
        {
            byte[] pdfDoc = Encoding.UTF8.GetBytes("");
            try
            {
                var connectOptions = new ConnectOptions
                {
                    BrowserWSEndpoint = await GetWebSocketDebuggerUrl()
                };
                var browser = await Puppeteer.ConnectAsync(connectOptions);

                using (var page = await browser.NewPageAsync())
                {
                    await page.SetContentAsync(htmlData);
                    
                    var pdfOp = new PdfOptions
                    {
                        Width = _pdfServiceConfig.PaperWidth,
                        Height = _pdfServiceConfig.PaperHeight,
                        MarginOptions = new MarginOptions
                        {
                            Top = _pdfServiceConfig.MarginTop,
                            Right = _pdfServiceConfig.MarginRight,
                            Bottom = _pdfServiceConfig.MarginBottom,
                            Left = _pdfServiceConfig.MarginLeft
                        },
                        PrintBackground = true
                    };
                    pdfDoc = await page.PdfDataAsync(pdfOp);
                }

                browser.Disconnect();

                return pdfDoc;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Puppeteer could not generate PDF");
                throw exception;
            }
        }

        private static FileContentResult GetPDFFromDisk(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            var file = File.ReadAllBytes(path);

            return new FileContentResult(file, "application/pdf");
        }

        private async Task<string> GetWebSocketDebuggerUrl()
        {
            var webSocketDebuggerUrl = await GetWebSocketDebuggerUrlFromCache();

            return await TestWebSocketDebuggerUrl(webSocketDebuggerUrl) ? webSocketDebuggerUrl : await GetWebSocketDebuggerUrlFromCache(true);
        }

        private async Task<string> GetWebSocketDebuggerUrlFromCache(bool clearCache = false)
        {
            if (clearCache)
                _memoryCache.Remove(WebSocketDebuggerUrlCacheKey);

            var webSocketDebuggerUrl = await _memoryCache.GetOrCreate(WebSocketDebuggerUrlCacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(CacheDurationDays);

                using var httpClient = new HttpClient();

                try
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);

                    var response = await httpClient.GetAsync(_pdfServiceConfig.ChromiumVersionUrl);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(responseBody);

                    return jObject?["webSocketDebuggerUrl"]?.Value<string>();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Could not get WebSocket Debugger URL for Chromium");
                    throw;
                }
            });

            _logger.LogInformation($"Got WebSocket Debugger URL: {webSocketDebuggerUrl}");

            return webSocketDebuggerUrl;
        }

        private async Task<bool> TestWebSocketDebuggerUrl(string webSocketDebuggerUrl)
        {
            using var socket = new ClientWebSocket();

            try
            {
                await socket.ConnectAsync(new Uri(webSocketDebuggerUrl), CancellationToken.None);
                socket.Abort();
                _logger.LogInformation("WebSocket Debugger URL tested OK");
                return true;
            }
            catch (Exception)
            {
                _logger.LogInformation("WebSocket Debugger URL tested NOT OK! ");
                return false;
            }
        }

        private void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Kunne ikke slette filen {path}");
                throw;
            }
        }

        private static string GetTempOutputPath()
        {
            return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        }
    }
}
