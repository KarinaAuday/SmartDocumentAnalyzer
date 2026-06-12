namespace SmartDocumentAnalyzer.Exceptions;

/// <summary>
/// Thrown when document analysis fails due to an AI provider error or invalid response.
/// Maps to HTTP 502 Bad Gateway in the global exception handler.
/// </summary>
public class AnalysisException : Exception
{
    public AnalysisException(string message) : base(message) { }

    public AnalysisException(string message, Exception innerException)
        : base(message, innerException) { }
}
