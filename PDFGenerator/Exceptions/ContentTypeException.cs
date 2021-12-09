namespace PdfGenerator.Exceptions
{
    public class ContentTypeException : Exception
    {
        public ContentTypeException()
        {
        }

        public ContentTypeException(string message) : base(message)
        {
        }

        public ContentTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
