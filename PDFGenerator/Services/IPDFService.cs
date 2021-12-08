using Microsoft.AspNetCore.Http;
using PdfGenerator.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PdfGenerator.Services
{
    public interface IPdfService
    {
        Task<PdfFile> GeneratePdfFromHtmlStringAsync(string htmlString, PdfCustomOptions options);
        Task<MemoryStream> GeneratePdfArchiveFromFilesAsync(List<IFormFile> files, PdfCustomOptions options);
    }
}
