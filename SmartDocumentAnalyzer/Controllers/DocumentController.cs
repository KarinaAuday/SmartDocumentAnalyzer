using Microsoft.AspNetCore.Mvc;
using SmartDocumentAnalyzer.Exceptions;
using SmartDocumentAnalyzer.Models;
using SmartDocumentAnalyzer.Services;

namespace SmartDocumentAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentController : ControllerBase
{
    private readonly IAIAnalysisService _analysisService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IAIAnalysisService analysisService, ILogger<DocumentController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a text document and returns a summary, key points, and sentiment.
    /// </summary>
    /// <param name="request">The document to analyze.</param>
    /// <returns>Structured analysis result.</returns>
    /// <response code="200">Analysis completed successfully.</response>
    /// <response code="400">The request body is invalid or DocumentText is empty.</response>
    /// <response code="502">The AI provider returned an unexpected response.</response>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Analyze([FromBody] AnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentText))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["DocumentText"] = ["DocumentText cannot be empty."] }
            });
        }

        const int maxLength = 10_000;
        if (request.DocumentText.Length > maxLength)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["DocumentText"] = [$"DocumentText cannot exceed {maxLength} characters."] }
            });
        }

        _logger.LogInformation("Analyzing document ({Length} chars)", request.DocumentText.Length);

        try
        {
            var result = await _analysisService.AnalyzeDocumentAsync(request.DocumentText);
            return Ok(result);
        }
        catch (AnalysisException ex)
        {
            _logger.LogWarning(ex, "Analysis provider error");

            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Analysis Provider Error",
                detail: ex.Message);
        }
    }
}
