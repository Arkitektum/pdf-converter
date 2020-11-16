using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDFGenerator.Models;
using PDFGenerator.Services;

namespace PDFGenerator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PDFController : ControllerBase
    {
        private readonly IPDFService _pdfService;

        public PDFController(IPDFService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost]
        [Authorize(Policy = "ApiKeyPolicy")]
        public async Task<IActionResult> GeneratePDF([FromBody] PDFGenerationOptions options)
        {
            try
            {
                var pdfDoc = await _pdfService.GeneratePdfAsync(options.HtmlData);
                Response.Headers.Add("Content-Type", "application/pdf");
                Response.Headers.Add("Content-Length", pdfDoc.Length.ToString());
                return File(pdfDoc, "application/pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
