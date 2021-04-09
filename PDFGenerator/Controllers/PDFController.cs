using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDFGenerator.Models;
using PDFGenerator.Services;
using System;
using System.Threading.Tasks;

namespace PDFGenerator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PDFController : ControllerBase
    {
        private readonly IPDFService _pdfService;

        public PDFController(
            IPDFService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost]
        [Authorize(Policy = "ApiKeyPolicy")]
        public async Task<IActionResult> GeneratePDF([FromBody] PDFGenerationOptions options)
        {
            try
            {
                var pdfData = await _pdfService.GeneratePDFAsync(options);

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
