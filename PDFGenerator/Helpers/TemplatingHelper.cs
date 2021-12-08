using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfGenerator.Helpers
{
    public class TemplatingHelper
    {
        private static readonly Assembly _executingAssembly = Assembly.GetExecutingAssembly();
        private static readonly Regex _templateRegex = new("{{(?<prop>(.*?))}}", RegexOptions.Compiled);

        public static async Task<string> StreamToStringAsync(Stream stream)
        {
            using var streamReader = new StreamReader(stream);

            return await streamReader.ReadToEndAsync();
        }

        public static string FormatHtml(string template, Dictionary<string, string> parameters)
        {
            return _templateRegex.Replace(template, match =>
            {
                var propName = match.Groups["prop"].Value;

                if (!parameters.TryGetValue(propName, out var value))
                    return match.Value;

                return value;
            });
        }

        public static async Task<string> GetTemplateAsync(string fileName)
        {
            var name = _executingAssembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(fileName));
            var stream = name != null ? _executingAssembly.GetManifestResourceStream(name) : null;

            if (stream == null)
                throw new Exception($"Kunne ikke finne template med navn '{fileName}' i assembly '{_executingAssembly.FullName}'.");

            return await StreamToStringAsync(stream);
        }
    }
}
