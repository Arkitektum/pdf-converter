using PuppeteerSharp;

namespace PdfGenerator.Services
{
    public interface IBrowserProvider
    {
        Task<Browser> GetBrowserAsync();
    }
}
