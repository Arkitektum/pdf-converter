using PdfGenerator.Models;

namespace PdfGenerator.Services
{
    public interface IPdfService
    {
        Task<PdfResult> GeneratePdfFromHtmlStringAsync(string htmlString, PdfOptions options);
        Task<PdfResult> GeneratePdfFromImageAsync(PdfInputData inputData, PdfOptions options);
    }
}
