using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartDocumentAnalyzer.Controllers;
using SmartDocumentAnalyzer.Exceptions;
using SmartDocumentAnalyzer.Models;
using SmartDocumentAnalyzer.Services;

namespace SmartDocumentAnalyzer.Tests.Controllers;

public class DocumentControllerTests
{
    private readonly Mock<IAIAnalysisService> _serviceMock = new();
    private readonly DocumentController _sut;

    public DocumentControllerTests()
    {
        _sut = new DocumentController(_serviceMock.Object, NullLogger<DocumentController>.Instance);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Analyze_ValidRequest_Returns200WithAnalysisResponse()
    {
        var expected = new AnalysisResponse
        {
            Summary = "Test summary",
            KeyPoints = ["Point A", "Point B"],
            Sentiment = "positive"
        };

        _serviceMock
            .Setup(s => s.AnalyzeDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(expected);

        var result = await _sut.Analyze(new AnalysisRequest { DocumentText = "Some valid text." });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AnalysisResponse>(ok.Value);
        Assert.Equal("Test summary", response.Summary);
        Assert.Equal("positive", response.Sentiment);
    }

    [Fact]
    public async Task Analyze_ValidRequest_PassesDocumentTextToService()
    {
        const string inputText = "This is my document text.";

        _serviceMock
            .Setup(s => s.AnalyzeDocumentAsync(inputText))
            .ReturnsAsync(new AnalysisResponse());

        await _sut.Analyze(new AnalysisRequest { DocumentText = inputText });

        // Verifica que el servicio fue llamado con exactamente el texto que recibió
        _serviceMock.Verify(s => s.AnalyzeDocumentAsync(inputText), Times.Once);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Analyze_EmptyDocumentText_Returns400()
    {
        var result = await _sut.Analyze(new AnalysisRequest { DocumentText = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Analyze_WhitespaceDocumentText_Returns400()
    {
        var result = await _sut.Analyze(new AnalysisRequest { DocumentText = "   " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Analyze_TextExceedsMaxLength_Returns400()
    {
        var tooLongText = new string('a', 10_001);

        var result = await _sut.Analyze(new AnalysisRequest { DocumentText = tooLongText });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Analyze_TextAtMaxLength_Returns200()
    {
        var maxText = new string('a', 10_000);

        _serviceMock
            .Setup(s => s.AnalyzeDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(new AnalysisResponse());

        var result = await _sut.Analyze(new AnalysisRequest { DocumentText = maxText });

        Assert.IsType<OkObjectResult>(result);
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public async Task Analyze_ServiceThrowsAnalysisException_Returns502()
    {
        _serviceMock
            .Setup(s => s.AnalyzeDocumentAsync(It.IsAny<string>()))
            .ThrowsAsync(new AnalysisException("Provider failed"));

        var result = await _sut.Analyze(new AnalysisRequest { DocumentText = "Some text" });

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
    }

    [Fact]
    public async Task Analyze_EmptyText_DoesNotCallService()
    {
        await _sut.Analyze(new AnalysisRequest { DocumentText = "" });

        // Si el input es inválido, el servicio nunca debe ser llamado
        _serviceMock.Verify(s => s.AnalyzeDocumentAsync(It.IsAny<string>()), Times.Never);
    }
}
