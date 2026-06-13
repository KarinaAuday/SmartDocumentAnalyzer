using SmartDocumentAnalyzer.Models;

namespace SmartDocumentAnalyzer.Services;

/// <summary>
/// Locally-executed document analysis — no API key required.
/// Uses word-frequency extraction for key points and a sentiment lexicon.
/// Suitable for demos and portfolio use.
/// </summary>
public class MockAnalysisService : IAIAnalysisService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","is","are","was","were","be","been","being","have","has","had",
        "do","does","did","will","would","could","should","may","might","can",
        "to","of","in","for","on","with","at","by","from","as","into","through",
        "during","before","after","above","below","between","out","off","over","under",
        "and","but","or","nor","not","so","yet","both","either","neither","each",
        "more","most","other","some","such","than","too","very","just","also",
        "this","that","these","those","it","its","he","she","they","we","you","i",
        "my","your","his","her","their","our","who","which","what","how","when","where","why",
        "all","any","because","if","while","about","against","then","once","here","there"
    };

    private static readonly HashSet<string> PositiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "good","great","excellent","best","amazing","wonderful","fantastic","positive",
        "success","successful","improve","improvement","benefit","advantage","innovative",
        "efficient","effective","outstanding","remarkable","valuable","strong","growth",
        "opportunity","progress","achieve","achieved","leading","innovative","robust"
    };

    private static readonly HashSet<string> NegativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bad","poor","worst","terrible","failure","problem","issue","concern","risk",
        "negative","difficult","hard","challenge","limitation","disadvantage",
        "ineffective","inefficient","inadequate","insufficient","lack","weak",
        "decline","loss","threat","error","mistake","fail","failed","crisis","damage"
    };

    public Task<AnalysisResponse> AnalyzeDocumentAsync(string documentText)
    {
        var words = Tokenize(documentText);
        var sentences = SplitSentences(documentText);

        var summary = BuildSummary(sentences, words.Count);
        var keyPoints = ExtractKeyPoints(words);
        var sentiment = DetectSentiment(documentText);

        return Task.FromResult(new AnalysisResponse
        {
            Summary = summary,
            KeyPoints = keyPoints,
            Sentiment = sentiment
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<string> Tokenize(string text) =>
        text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']'))
            .Where(w => w.Length > 0)
            .ToList();

    private static List<string> SplitSentences(string text) =>
        text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 10)
            .ToList();

    private static string BuildSummary(List<string> sentences, int totalWords)
    {
        if (sentences.Count == 0)
            return "No sentences detected.";

        // Score each sentence by density of meaningful words (extractive summarization)
        var wordFreq = sentences
            .SelectMany(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(w => w.Trim('.', ',', '!', '?', ';', ':', '"', '\'').ToLower())
            .Where(w => w.Length > 3 && !StopWords.Contains(w) && w.All(char.IsLetter))
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count());

        var scored = sentences
            .Select((s, i) => new
            {
                Sentence = s,
                Index = i,
                Score = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Select(w => w.Trim('.', ',', '!', '?', ';', ':', '"', '\'').ToLower())
                         .Sum(w => wordFreq.TryGetValue(w, out var freq) ? freq : 0)
            })
            .OrderByDescending(x => x.Score)
            .Take(2)
            .OrderBy(x => x.Index) // restore original order
            .ToList();

        var summary = string.Join(" ", scored.Select(x => x.Sentence.Trim()));
        return summary;
    }

    private static List<string> ExtractKeyPoints(List<string> words)
    {
        var keyTerms = words
            .Select(w => w.ToLower())
            .Where(w => w.Length > 3 && !StopWords.Contains(w) && w.All(char.IsLetter))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g =>
            {
                var term = $"{char.ToUpper(g.Key[0])}{g.Key[1..]}";
                return $"{term} — appears {g.Count()} time{(g.Count() > 1 ? "s" : "")}";
            })
            .ToList<string>();

        return keyTerms.Count > 0
            ? keyTerms
            : ["No significant key terms identified in the provided text."];
    }

    private static string DetectSentiment(string text)
    {
        var lower = text.ToLower();
        var posScore = PositiveWords.Count(w => lower.Contains(w));
        var negScore = NegativeWords.Count(w => lower.Contains(w));

        return posScore > negScore ? "positive"
             : negScore > posScore ? "negative"
             : "neutral";
    }
}
