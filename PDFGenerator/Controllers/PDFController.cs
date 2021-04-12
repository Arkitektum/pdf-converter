using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<PdfController> _logger;

        public PdfController(
            IPdfService pdfService,
            ILogger<PdfController> logger)
        {
            _pdfService = pdfService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "ApiKeyPolicy")]
        public async Task<IActionResult> GeneratePdf([FromBody] PdfGenerationOptions options)
        {
            try
            {
                if (options?.HtmlData == null)
                    return BadRequest();

                var pdfFile = await _pdfService.GeneratePdfAsync(options);

                Response.Headers.Add("Content-Type", PdfFile.ContentType);
                Response.Headers.Add("Content-Length", pdfFile.FileSize.ToString());

                return File(pdfFile.Data, PdfFile.ContentType, pdfFile.FileName);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "En feil har oppstått!");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
