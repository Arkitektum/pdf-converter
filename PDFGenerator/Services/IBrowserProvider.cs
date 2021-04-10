using PuppeteerSharp;
using System.Threading.Tasks;

namespace PdfGenerator.Services
{
    public interface IBrowserProvider
    {
        Task<Browser> GetBrowser();
    }
}
