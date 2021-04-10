using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PdfGenerator.Models;
using PdfGenerator.Services;
using System;
using System.Threading.Tasks;

namespace PdfGenerator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IPdfService _pdfService;

        public PdfController(
            IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost]
        [Authorize(Policy = "ApiKeyPolicy")]
        public async Task<IActionResult> GeneratePdf([FromBody] PdfGenerationOptions options)
        {
            try
            {
                var pdfData = await _pdfService.GeneratePdfAsync(options);

                Response.Headers.Add("Content-Type", "application/pdf");
                Response.Headers.Add("Content-Length", pdfData.Length.ToString());

                return File(pdfData, "application/pdf", options.FileName ?? $"{Guid.NewGuid()}.pdf");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
