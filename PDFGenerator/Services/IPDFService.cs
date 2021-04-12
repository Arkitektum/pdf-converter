using PdfGenerator.Models;
using System.Threading.Tasks;

namespace PdfGenerator.Services
{
    public interface IPdfService
    {
        Task<PdfFile> GeneratePdfAsync(PdfGenerationOptions options);
    }
}
