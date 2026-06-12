# SmartDocumentAnalyzer — Resumen para Entrevistas

## ¿Qué es este proyecto?

Una **REST API** construida con **ASP.NET Core Web API (.NET 10)** que recibe un documento de texto, lo analiza con inteligencia artificial, y devuelve un resumen estructurado con: resumen del contenido, puntos clave, y sentimiento (positivo / neutro / negativo).

---

## ¿Qué tecnologías usé y por qué?

| Tecnología | Por qué |
|---|---|
| **ASP.NET Core .NET 10** | Framework moderno de Microsoft para APIs, rendimiento top, multiplataforma |
| **Anthropic Claude API** | LLM de Anthropic, integrado via SDK oficial (`Anthropic.SDK`) |
| **OpenAPI / Swagger** | Documentación automática del endpoint, facilita testing y onboarding |
| **ProblemDetails (RFC 7807)** | Estándar para respuestas de error en REST APIs — consistente y tipado |

---

## Patrones y decisiones de diseño

### Strategy Pattern (el más importante para mencionar)

```
IAIAnalysisService
    ├── MockAnalysisService    (análisis local, sin API key)
    └── ClaudeAnalysisService  (Anthropic Claude API)
```

El `DocumentController` **solo conoce la interfaz** `IAIAnalysisService`. No sabe si está hablando con Claude o con el servicio local. Eso significa que puedo agregar OpenAI, Gemini, o cualquier otro proveedor **sin tocar el controller ni la interfaz** — solo creo una nueva clase y cambio el registro en `Program.cs`.

> **Cómo explicarlo en entrevista:** "Usé el Strategy Pattern para desacoplar el proveedor de IA del resto de la aplicación. El controller depende de una abstracción, no de una implementación concreta. Esto respeta el principio Open/Closed de SOLID: abierto para extensión, cerrado para modificación."

### Dependency Injection (DI)

El servicio se registra en `Program.cs` según configuración:

```csharp
if (provider == "Claude")
    services.AddScoped<IAIAnalysisService, ClaudeAnalysisService>();
else
    services.AddScoped<IAIAnalysisService, MockAnalysisService>();
```

El controller recibe el servicio por constructor — nunca lo instancia directamente. Esto facilita testing (podés mockear la interfaz) y mantiene bajo acoplamiento.

### Manejo de errores en capas

Hay tres capas de manejo de errores:

1. **Validación de input** en el controller → `400 Bad Request` con detalle de qué campo falló
2. **AnalysisException** — excepción custom que lanza el servicio si la IA falla → `502 Bad Gateway`
3. **Global exception handler** en `Program.cs` → captura cualquier excepción no manejada y devuelve `ProblemDetails` estándar

Todas las respuestas de error tienen el mismo formato JSON:
```json
{
  "status": 502,
  "title": "Analysis Provider Error",
  "detail": "Claude returned a response that could not be parsed.",
  "instance": "/api/document/analyze",
  "traceId": "0HN5..."
}
```

> **Por qué ProblemDetails:** Es el estándar RFC 7807 para errores en REST APIs. Cualquier cliente (front, mobile, otro servicio) sabe exactamente cómo parsear el error sin adivinar el formato.

---

## Flujo completo de una request

```
1. Cliente hace POST /api/document/analyze
   Body: { "documentText": "..." }

2. DocumentController.Analyze()
   ├─ Valida que el texto no esté vacío ni supere 10.000 chars
   └─ Llama a IAIAnalysisService.AnalyzeDocumentAsync()

3. MockAnalysisService (o ClaudeAnalysisService)
   ├─ Mock: tokeniza, extrae términos por frecuencia, detecta sentimiento por lexicón
   └─ Claude: envía prompt estructurado a la API, parsea respuesta JSON

4. Retorna AnalysisResponse
   {
     "summary": "...",
     "keyPoints": ["...", "...", "..."],
     "sentiment": "positive|neutral|negative"
   }
```

---

## Preguntas frecuentes en entrevistas

**¿Por qué dos servicios (Mock y Claude)?**
El Mock permite que cualquiera pueda probar la API sin costo ni configuración — ideal para demos en portfolio. Claude es el modo "producción". Cambiar entre ellos es una línea en `appsettings.json`.

**¿Cómo testearías este proyecto?**
La interfaz `IAIAnalysisService` facilita unit testing: en los tests del controller, inyecto un mock de la interfaz con Moq. El servicio real (`ClaudeAnalysisService`) lo testeo con integration tests que golpean la API real o un server fake.

**¿Cómo escalarías esto?**
- Agregar rate limiting (middleware de .NET 7+)
- Cachear respuestas por hash del documento (evita llamadas duplicadas a la IA)
- Publicar en Azure App Service o contenedor Docker
- Agregar autenticación JWT para controlar acceso

**¿Qué harías diferente en producción?**
- API key nunca en `appsettings.json` → Azure Key Vault o variables de entorno
- Logging estructurado con Serilog hacia Application Insights
- Health check endpoint (`/health`)
- Circuit breaker para la llamada a la API de IA (con Polly)

---

## Estructura del proyecto

```
SmartDocumentAnalyzer/
├── Controllers/
│   └── DocumentController.cs     ← endpoint POST /api/document/analyze
├── Exceptions/
│   └── AnalysisException.cs      ← excepción custom para errores de IA
├── Models/
│   ├── AnalysisRequest.cs        ← input: DocumentText
│   └── AnalysisResponse.cs       ← output: Summary, KeyPoints, Sentiment
├── Services/
│   ├── IAIAnalysisService.cs     ← contrato / interfaz
│   ├── MockAnalysisService.cs    ← análisis local (gratis, sin API key)
│   └── ClaudeAnalysisService.cs  ← Anthropic Claude API
├── appsettings.json              ← AnalysisProvider: "Mock" | "Claude"
└── Program.cs                    ← DI, error handling global
```
