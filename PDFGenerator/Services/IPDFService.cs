using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PDFGenerator.Services
{
    public interface IPDFService
    {
        Task<byte[]> GeneratePdfAsync(string htmlData);
    }
}
