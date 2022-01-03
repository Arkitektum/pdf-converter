using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfGenerator.Helpers;
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
        private readonly IMultipartRequestService _multipartRequestService;
        private readonly ILogger<PdfController> _logger;

        public PdfController(
            IPdfService pdfService,
            IMultipartRequestService multipartRequestService,
            ILogger<PdfController> logger)
        {
            _pdfService = pdfService;
            _multipartRequestService = multipartRequestService;
            _logger = logger;
        }

        [HttpPost("fromHtmlString")]
        public async Task<IActionResult> GenerateFromHtmlString([ModelBinder(BinderType = typeof(JsonModelBinder))] PdfOptions options, [FromForm] string htmlString)
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

        [HttpPost("fromImage")]
        [RequestFormLimits(MultipartBodyLengthLimit = 25_000_000)]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> GenerateFromImage([ModelBinder(BinderType = typeof(JsonModelBinder))] PdfOptions options)
        {
            try
            {
                using var inputData = await _multipartRequestService.GetPdfInputData();

                if (inputData?.File == null)
                    return BadRequest();

                if (!ImageHelper.IsImage(inputData.File.ContentType))
                    return BadRequest($@"Ugyldig MIME-type ""{inputData.File.ContentType}"".");

                var pdfResult = await _pdfService.GeneratePdfFromImageAsync(inputData, options);

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
    }
}
