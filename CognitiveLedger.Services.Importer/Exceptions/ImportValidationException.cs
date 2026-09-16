using CognitiveLedger.Common.Request;

namespace CognitiveLedger.Services.Importer.Exceptions;

public class ImportValidationException : Exception
{
    public ImportValidationException()
    {
        
    }
    public ImportValidationException(RequestBase request, string message)
        : base($"{request}: {message}")
    {
    }
}