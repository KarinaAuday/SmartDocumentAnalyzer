using Scalar.AspNetCore;
using SmartDocumentAnalyzer.Exceptions;
using SmartDocumentAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ProblemDetails: standardizes all error responses as RFC 7807 JSON
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

// Strategy: swap AI provider via appsettings.json "AnalysisProvider" key
// "Mock"  → local NLP analysis, no API key needed (default, good for demos)
// "Claude" → Anthropic Claude API (requires Anthropic:ApiKey in config)
var provider = builder.Configuration["AnalysisProvider"] ?? "Mock";

if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IAIAnalysisService, ClaudeAnalysisService>();
else
    builder.Services.AddScoped<IAIAnalysisService, MockAnalysisService>();

// ── App pipeline ─────────────────────────────────────────────────────────────

var app = builder.Build();

// Global exception handler: maps exceptions → ProblemDetails responses
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var (status, title) = ex switch
        {
            AnalysisException   => (StatusCodes.Status502BadGateway, "Analysis Provider Error"),
            ArgumentException   => (StatusCodes.Status400BadRequest, "Invalid Argument"),
            _                   => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex?.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
