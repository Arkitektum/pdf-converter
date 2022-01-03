namespace PdfGenerator.Models
{
    public sealed class PdfInputData : IDisposable
    {
        public IFormFile File { get; set; }
        public string Title { get; set; }

        public void Dispose() => File.OpenReadStream().Dispose();
    }
}
