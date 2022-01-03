using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using PdfGenerator.Helpers;
using PdfGenerator.Models;
using System.Text;

namespace PdfGenerator.Services
{
    public class MultipartRequestService : IMultipartRequestService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MultipartRequestService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PdfInputData> GetPdfInputData()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var reader = new MultipartReader(request.GetMultipartBoundary(), request.Body);
            var formAccumulator = new KeyValueAccumulator();
            IFormFile imageFile = null;
            MultipartSection section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                    continue;

                if (contentDisposition.IsFormDisposition())
                    formAccumulator = await AccumulateForm(formAccumulator, section, contentDisposition);

                else if (contentDisposition.IsFileDisposition() && contentDisposition.Name.Value == "image" && imageFile == null && ImageHelper.IsImage(section.ContentType))
                    imageFile = await CreateFormFile(contentDisposition, section);
            }

            if (imageFile == null)
                return null;

            var accumulatedValues = formAccumulator.GetResults();

            return new PdfInputData
            {
                File = imageFile,
                Title = accumulatedValues.ContainsKey("title") ? accumulatedValues["title"].ToString() : null
            };
        }

        private static async Task<KeyValueAccumulator> AccumulateForm(
            KeyValueAccumulator formAccumulator, MultipartSection section, ContentDispositionHeaderValue contentDisposition)
        {
            var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;

            using var streamReader = new StreamReader(section.Body, GetEncoding(section), true, 1024, true);
            {
                var value = await streamReader.ReadToEndAsync();

                if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                    value = string.Empty;

                formAccumulator.Append(key, value);

                if (formAccumulator.ValueCount > FormReader.DefaultValueCountLimit)
                    throw new InvalidDataException($"Form key count limit {FormReader.DefaultValueCountLimit} exceeded.");
            }

            return formAccumulator;
        }

        private static async Task<IFormFile> CreateFormFile(ContentDispositionHeaderValue contentDisposition, MultipartSection section)
        {
            var memoryStream = new MemoryStream();
            await section.Body.CopyToAsync(memoryStream);
            await section.Body.DisposeAsync();
            memoryStream.Position = 0;

            return new FormFile(memoryStream, 0, memoryStream.Length, contentDisposition.Name.ToString(), contentDisposition.FileName.ToString())
            {
                Headers = new HeaderDictionary(),
                ContentType = section.ContentType
            };
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

            #pragma warning disable SYSLIB0001
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
                return Encoding.UTF8;
            #pragma warning restore SYSLIB0001

            return mediaType.Encoding;
        }
    }
}
