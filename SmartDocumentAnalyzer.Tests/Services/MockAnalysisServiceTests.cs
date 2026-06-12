using SmartDocumentAnalyzer.Services;

namespace SmartDocumentAnalyzer.Tests.Services;

public class MockAnalysisServiceTests
{
    private readonly MockAnalysisService _sut = new();

    // ── Sentiment ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_PositiveText_ReturnsSentimentPositive()
    {
        var result = await _sut.AnalyzeDocumentAsync(
            "This is an excellent and outstanding achievement. The growth has been great and the results are amazing.");

        Assert.Equal("positive", result.Sentiment);
    }

    [Fact]
    public async Task AnalyzeAsync_NegativeText_ReturnsSentimentNegative()
    {
        var result = await _sut.AnalyzeDocumentAsync(
            "The project was a terrible failure. There are serious problems and the results are poor and inadequate.");

        Assert.Equal("negative", result.Sentiment);
    }

    [Fact]
    public async Task AnalyzeAsync_NeutralText_ReturnsSentimentNeutral()
    {
        var result = await _sut.AnalyzeDocumentAsync(
            "The document contains information about processes and procedures followed by the team.");

        Assert.Equal("neutral", result.Sentiment);
    }

    // ── Key Points ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_ReturnsAtLeastOneKeyPoint()
    {
        var result = await _sut.AnalyzeDocumentAsync("Artificial intelligence is transforming industries worldwide.");

        Assert.NotEmpty(result.KeyPoints);
    }

    [Fact]
    public async Task AnalyzeAsync_RepeatedWord_AppearsInKeyPoints()
    {
        // "cloud" appears 4 times — should be extracted as a key term
        var result = await _sut.AnalyzeDocumentAsync(
            "Cloud computing is the future. Cloud services are growing. Cloud adoption is accelerating. Cloud technology improves efficiency.");

        Assert.Contains(result.KeyPoints, kp => kp.Contains("Cloud", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsMaxFiveKeyPoints()
    {
        var result = await _sut.AnalyzeDocumentAsync(
            "Technology innovation drives transformation. Digital solutions enable enterprise growth. Data analytics powers decisions. Automation increases productivity. Security protects infrastructure.");

        Assert.True(result.KeyPoints.Count <= 5);
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_ReturnsSummaryWithWordCount()
    {
        var result = await _sut.AnalyzeDocumentAsync(
            "Artificial intelligence is transforming the world. Many industries are adopting AI solutions.");

        Assert.Contains("words", result.Summary);
    }

    // ── Edge Cases ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_SingleWord_DoesNotThrow()
    {
        var result = await _sut.AnalyzeDocumentAsync("Hello");

        Assert.NotNull(result);
        Assert.NotNull(result.Summary);
        Assert.NotNull(result.KeyPoints);
        Assert.NotNull(result.Sentiment);
    }

    [Fact]
    public async Task AnalyzeAsync_ShortText_ReturnsStructuredResponse()
    {
        var result = await _sut.AnalyzeDocumentAsync("This is a short document.");

        Assert.NotNull(result.Summary);
        Assert.NotEmpty(result.Sentiment);
    }
}
