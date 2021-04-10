using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System;
using System.Threading.Tasks;

namespace PdfGenerator.Services
{
    public class BrowserProvider : IBrowserProvider
    {
        private Browser _browser;
        private readonly PdfServiceConfig _config;
        private readonly ILogger<BrowserProvider> _logger;

        public BrowserProvider(
            IOptions<PdfServiceConfig> options,
            ILogger<BrowserProvider> logger)
        {
            _config = options.Value;
            _logger = logger;
        }

        public async Task<Browser> GetBrowser()
        {
            if (_browser != null && _browser.IsConnected)
                return _browser;

            await ConnectToBrowser();

            return _browser;
        }

        private async Task ConnectToBrowser()
        {
            try
            {
                _browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserURL = _config.ChromiumVersionUrl });
                _logger.LogInformation($"Koblet til nettleser: {_browser.WebSocketEndpoint}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kunne ikke koble til nettleser");
                throw;
            }
        }
    }
}
