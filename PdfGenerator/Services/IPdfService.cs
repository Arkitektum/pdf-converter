using PdfGenerator.Models;

namespace PdfGenerator.Services
{
    public interface IPdfService
    {
        Task<PdfResult> GeneratePdfFromHtmlStringAsync(string htmlString, PdfOptions options);
        Task<PdfResult> GeneratePdfFromFileAsync(IFormFile file, PdfOptions options);
        Task<MemoryStream> GeneratePdfZipFromFilesAsync(List<IFormFile> files, PdfOptions options);
    }
}
