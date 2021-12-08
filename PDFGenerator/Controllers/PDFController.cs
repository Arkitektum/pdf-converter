using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PdfGenerator.Models;
using PdfGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PdfGenerator.Controllers
{
    [ApiController]
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

        [HttpPost("fromString")]
        [Authorize(Policy = "ApiKeyPolicy")]
        public async Task<IActionResult> GenerateFromString(
            [ModelBinder(BinderType = typeof(JsonModelBinder))] PdfCustomOptions options, string htmlString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(htmlString))
                    return BadRequest();

                var pdfFile = await _pdfService.GeneratePdfFromHtmlStringAsync(htmlString, options);

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

        [HttpPost("fromFiles")]
        public async Task<IActionResult> GenerateFromFiles(
            [ModelBinder(BinderType = typeof(JsonModelBinder))] PdfCustomOptions options, List<IFormFile> files)
        {
            try
            {
                if (!files?.Any() ?? true)
                    return BadRequest();

                var pdfArchive = await _pdfService.GeneratePdfArchiveFromFilesAsync(files, options);

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
