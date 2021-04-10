using PdfGenerator.Models;
using System.Threading.Tasks;

namespace PdfGenerator.Services
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePdfAsync(PdfGenerationOptions options);
    }
}
