using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace PdfGenerator.Services
{
    public class BrowserProvider : IBrowserProvider
    {
        private Browser _browser;
        private readonly PdfSettings _config;
        private readonly ILogger<BrowserProvider> _logger;

        public BrowserProvider(
            IOptions<PdfSettings> options,
            ILogger<BrowserProvider> logger)
        {
            _config = options.Value;
            _logger = logger;
        }

        public async Task<Browser> GetBrowserAsync()
        {
            if (_browser?.IsConnected ?? false)
                return _browser;

            await Connect();

            return _browser;
        }

        private async Task Connect()
        {
            try
            {
                if (_browser != null)
                    await _browser.DisposeAsync();

                _browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserURL = _config.ChromiumVersionUrl });
                _logger.LogInformation("Koblet til nettleser: {webSocketEndpoint}", _browser.WebSocketEndpoint);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kunne ikke koble til nettleser");
                throw;
            }
        }
    }
}
