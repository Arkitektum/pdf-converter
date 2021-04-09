using PDFGenerator.Models;
using System.Threading.Tasks;

namespace PDFGenerator.Services
{
    public interface IPDFService
    {
        Task<byte[]> GeneratePDFAsync(PDFGenerationOptions options);
    }
}
