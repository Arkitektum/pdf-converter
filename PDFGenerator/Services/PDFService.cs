using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PDFGenerator.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<byte[]> GeneratePDFAsync(PDFGenerationOptions options)
        {
            try
            {
                return await GeneratePdfAsync(options);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not generate PDF!");

                return null;
            }
        }

        private async Task<byte[]> GeneratePdfAsync(PDFGenerationOptions options)
        {
            try
            {
                byte[] pdfData = null;
                var connectOptions = new ConnectOptions { BrowserWSEndpoint = await GetWebSocketDebuggerUrl() };
                var browser = await Puppeteer.ConnectAsync(connectOptions);

                using (var page = await browser.NewPageAsync())
                {
                    await page.SetContentAsync(options.HtmlData);

                    var pdfOptions = new PdfOptions
                    {
                        Width = options.Paper.PaperWidth ?? _pdfServiceConfig.PaperWidth,
                        Height = options.Paper.PaperHeight ?? _pdfServiceConfig.PaperHeight,
                        MarginOptions = new MarginOptions
                        {
                            Top = options.Paper.MarginTop ?? _pdfServiceConfig.MarginTop,
                            Right = options.Paper.MarginRight ?? _pdfServiceConfig.MarginRight,
                            Bottom = options.Paper.MarginBottom ?? _pdfServiceConfig.MarginBottom,
                            Left = options.Paper.MarginLeft ?? _pdfServiceConfig.MarginLeft
                        },
                        PrintBackground = true
                    };

                    pdfData = await page.PdfDataAsync(pdfOptions);
                }

                browser.Disconnect();

                return pdfData;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Puppeteer could not generate PDF");
                throw;
            }
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
    }
}
