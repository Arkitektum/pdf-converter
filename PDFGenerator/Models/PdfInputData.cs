using Microsoft.AspNetCore.Http;

namespace PdfGenerator.Models
{
    public class PdfInputData
    {
        public string Name { get; set; }
        public IFormFile File { get; set; }
        public PdfInputDataType Type { get; set; }
    }

    public enum PdfInputDataType
    {
        HTML,
        Image
    }
}
