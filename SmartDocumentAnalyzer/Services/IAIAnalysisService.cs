using SmartDocumentAnalyzer.Models;

namespace SmartDocumentAnalyzer.Services;

public interface IAIAnalysisService
{
    Task<AnalysisResponse> AnalyzeDocumentAsync(string documentText);
}
