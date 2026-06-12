# SmartDocumentAnalyzer

REST API built with **ASP.NET Core (.NET 10)** that analyzes text documents using AI and returns a structured response with summary, key points, and sentiment.

> Works out of the box with no API key — switch to Claude AI with a single config change.

---

## Features

- `POST /api/document/analyze` — accepts plain text, returns structured JSON analysis
- **Two analysis modes**: local NLP (free, no setup) or Anthropic Claude API
- **Strategy Pattern** — swap AI providers without touching the controller
- **Global error handling** — RFC 7807 ProblemDetails on all errors
- **Unit tested** — xUnit + Moq covering services and controller

---

## Quick Start

### 1. Clone and run

```bash
git clone https://github.com/YOUR_USERNAME/SmartDocumentAnalyzer.git
cd SmartDocumentAnalyzer
dotnet run --project SmartDocumentAnalyzer
```

### 2. Test the endpoint

Open Swagger at `https://localhost:{port}/openapi/v1.json` or use curl:

```bash
curl -X POST https://localhost:5001/api/document/analyze \
  -H "Content-Type: application/json" \
  -d '{"documentText": "Artificial intelligence is transforming industries worldwide. Companies are investing heavily in AI to improve efficiency and reduce costs."}'
```

**Response:**
```json
{
  "summary": "Artificial intelligence is transforming industries worldwide. [Analyzed 24 words across 2 sentences.]",
  "keyPoints": [
    "Intelligence — appears 1 time",
    "Transforming — appears 1 time",
    "Industries — appears 1 time"
  ],
  "sentiment": "positive"
}
```

---

## Configuration

The analysis provider is set in `appsettings.json`:

```json
"AnalysisProvider": "Mock"
```

| Value | Description | Requires |
|-------|-------------|----------|
| `"Mock"` | Local NLP analysis — word frequency + sentiment lexicon | Nothing |
| `"Claude"` | Anthropic Claude API | API key (see below) |

### Enable Claude AI

1. Change `"AnalysisProvider": "Mock"` → `"Claude"` in `appsettings.json`
2. Set your API key (use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — never commit keys):

```bash
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
```

Get a free API key at [console.anthropic.com](https://console.anthropic.com).

---

## Architecture

```
Controllers/
└── DocumentController       ← POST /api/document/analyze

Exceptions/
└── AnalysisException        ← custom exception → HTTP 502

Models/
├── AnalysisRequest          ← input: DocumentText (string)
└── AnalysisResponse         ← output: Summary, KeyPoints, Sentiment

Services/
├── IAIAnalysisService       ← interface (Strategy Pattern)
├── MockAnalysisService      ← local NLP, no API key needed
└── ClaudeAnalysisService    ← Anthropic Claude API
```

**Design decisions:**

- `DocumentController` depends on `IAIAnalysisService` (interface), never on a concrete implementation — follows the Open/Closed principle
- Provider is swapped at startup via configuration, demonstrating dependency injection
- Error handling uses three layers: input validation (400), `AnalysisException` (502), and a global fallback handler for unhandled exceptions (500)

---

## Running Tests

```bash
dotnet test SmartDocumentAnalyzer.Tests
```

Tests cover:
- Sentiment detection (positive / negative / neutral)
- Key term extraction and limits
- Controller validation (empty input, max length)
- Controller error handling (502 on `AnalysisException`)
- Service is never called on invalid input

---

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- [Anthropic.SDK](https://github.com/tghamm/Anthropic.SDK) — .NET client for Claude API
- xUnit + Moq — unit testing
- OpenAPI (built-in .NET 10)

---

## License

MIT
