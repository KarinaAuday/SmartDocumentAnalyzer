using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using SmartDocumentAnalyzer.Exceptions;
using SmartDocumentAnalyzer.Models;
using System.Text.Json;

namespace SmartDocumentAnalyzer.Services;

/// <summary>
/// Document analysis powered by Anthropic Claude API.
/// Requires "Anthropic:ApiKey" set in configuration (appsettings or User Secrets).
/// </summary>
public class ClaudeAnalysisService : IAIAnalysisService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeAnalysisService> _logger;

    public ClaudeAnalysisService(IConfiguration configuration, ILogger<ClaudeAnalysisService> logger)
    {
        _logger = logger;

        var apiKey = configuration["Anthropic:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            throw new InvalidOperationException(
                "Anthropic API key is not configured. " +
                "Set 'Anthropic:ApiKey' in appsettings.json or via User Secrets.");

        _client = new AnthropicClient(apiKey);
    }

    public async Task<AnalysisResponse> AnalyzeDocumentAsync(string documentText)
    {
        _logger.LogInformation("Sending document to Claude for analysis ({Length} chars)", documentText.Length);

        var prompt = $"""
            Analyze the following document and respond ONLY with a valid JSON object.
            Do not include markdown, code fences, or any text outside the JSON.

            Use exactly this structure:
            {{
              "summary": "A concise 2-3 sentence summary of the document",
              "keyPoints": ["Key insight 1", "Key insight 2", "Key insight 3"],
              "sentiment": "positive|neutral|negative"
            }}

            Document:
            {documentText}
            """;

        try
        {
            var parameters = new MessageParameters
            {
                Model = AnthropicModels.Claude3Sonnet,
                MaxTokens = 1024,
                Messages = [new Message { Role = RoleType.User, Content = prompt }]
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var rawJson = response.Content[0].Text;

            _logger.LogDebug("Claude raw response: {Response}", rawJson);

            var result = JsonSerializer.Deserialize<AnalysisResponse>(
                rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? throw new AnalysisException("Claude returned an empty response.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Claude response");
            throw new AnalysisException("Claude returned a response that could not be parsed as JSON.", ex);
        }
        catch (Exception ex) when (ex is not AnalysisException)
        {
            _logger.LogError(ex, "Unexpected error calling Claude API");
            throw new AnalysisException("An error occurred while communicating with the Claude API.", ex);
        }
    }
}
