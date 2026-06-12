namespace SmartDocumentAnalyzer.Models;

public class AnalysisResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = [];
    public string Sentiment { get; set; } = string.Empty;
}
