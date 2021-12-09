using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfGenerator.Models;
using PdfGenerator.Services;

namespace PdfGenerator.Controllers
{
    [ApiController]
    [Authorize(Policy = "ApiKeyPolicy")]
    [Route("pdf")]
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

        [HttpPost("fromHtmlString")]
        public async Task<IActionResult> GenerateFromHtmlString(
            [ModelBinder(BinderType = typeof(JsonModelBinder))] PdfOptions options, [FromForm] string htmlString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(htmlString))
                    return BadRequest();

                var pdfResult = await _pdfService.GeneratePdfFromHtmlStringAsync(htmlString, options);

                Response.Headers.Add("Content-Type", PdfResult.ContentType);
                Response.Headers.Add("Content-Length", pdfResult.FileSize.ToString());

                return File(pdfResult.Data, PdfResult.ContentType, pdfResult.FileName);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "En feil har oppstått!");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("fromFile")]
        public async Task<IActionResult> GenerateFromFile(
            [ModelBinder(BinderType = typeof(JsonModelBinder))] PdfOptions options, IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest();

                var pdfResult = await _pdfService.GeneratePdfFromFileAsync(file, options);

                Response.Headers.Add("Content-Type", PdfResult.ContentType);
                Response.Headers.Add("Content-Length", pdfResult.FileSize.ToString());

                return File(pdfResult.Data, PdfResult.ContentType, pdfResult.FileName);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "En feil har oppstått!");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("fromFiles")]
        public async Task<IActionResult> GenerateFromFiles(
            [ModelBinder(BinderType = typeof(JsonModelBinder))] PdfOptions options, List<IFormFile> files)
        {
            try
            {
                if (!files?.Any() ?? true)
                    return BadRequest();

                var pdfArchive = await _pdfService.GeneratePdfZipFromFilesAsync(files, options);

                Response.Headers.Add("Content-Type", "application/zip");
                Response.Headers.Add("Content-Length", pdfArchive.Length.ToString());

                return File(pdfArchive.ToArray(), "application/zip", $"pdf-documents-{DateTime.Now:yyyyMMddTHHmmss}.zip");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "En feil har oppstått!");

                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
