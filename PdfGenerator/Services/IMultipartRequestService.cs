using PdfGenerator.Models;

namespace PdfGenerator.Services
{
    public interface IMultipartRequestService
    {
        Task<PdfInputData> GetPdfInputData();
    }
}
