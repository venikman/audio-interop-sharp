using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioSharp.App.Components;
using AudioSharp.App.Data;
using AudioSharp.App.Models;
using AudioSharp.App.Options;
using AudioSharp.App.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 32 * 1024 * 1024;
});

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024 * 1024;
});

builder.Services.Configure<CircuitOptions>(options =>
{
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("AudioSharp"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation(options => options.RecordException = true)
            .AddConsoleExporter();
    });

var openRouterEnvOverrides = new Dictionary<string, string?>();
var openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (!string.IsNullOrWhiteSpace(openRouterApiKey))
{
    openRouterEnvOverrides[$"{OpenRouterOptions.SectionName}:ApiKey"] = openRouterApiKey;
}

var openRouterModel = Environment.GetEnvironmentVariable("OPENROUTER_MODEL");
var openRouterAudioModel = Environment.GetEnvironmentVariable("OPENROUTER_AUDIO_MODEL");
var openRouterTextModel = Environment.GetEnvironmentVariable("OPENROUTER_TEXT_MODEL");

if (!string.IsNullOrWhiteSpace(openRouterAudioModel))
{
    openRouterEnvOverrides[$"{OpenRouterOptions.SectionName}:AudioModel"] = openRouterAudioModel;
}
else if (!string.IsNullOrWhiteSpace(openRouterModel))
{
    openRouterEnvOverrides[$"{OpenRouterOptions.SectionName}:AudioModel"] = openRouterModel;
}

if (!string.IsNullOrWhiteSpace(openRouterTextModel))
{
    openRouterEnvOverrides[$"{OpenRouterOptions.SectionName}:TextModel"] = openRouterTextModel;
}
else if (!string.IsNullOrWhiteSpace(openRouterModel))
{
    openRouterEnvOverrides[$"{OpenRouterOptions.SectionName}:TextModel"] = openRouterModel;
}

if (openRouterEnvOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(openRouterEnvOverrides);
}

var fhirEnvOverrides = new Dictionary<string, string?>();
var fhirBaseUrl = Environment.GetEnvironmentVariable("FHIR_SERVER_BASE_URL");
if (!string.IsNullOrWhiteSpace(fhirBaseUrl))
{
    fhirEnvOverrides[$"{FhirServerOptions.SectionName}:BaseUrl"] = fhirBaseUrl;
}

var fhirToken = Environment.GetEnvironmentVariable("FHIR_SERVER_BEARER_TOKEN")
    ?? Environment.GetEnvironmentVariable("FHIR_SERVER_TOKEN");
if (!string.IsNullOrWhiteSpace(fhirToken))
{
    fhirEnvOverrides[$"{FhirServerOptions.SectionName}:BearerToken"] = fhirToken;
}

var fhirBundleEndpoint = Environment.GetEnvironmentVariable("FHIR_SERVER_BUNDLE_ENDPOINT");
if (!string.IsNullOrWhiteSpace(fhirBundleEndpoint))
{
    fhirEnvOverrides[$"{FhirServerOptions.SectionName}:BundleEndpoint"] = fhirBundleEndpoint;
}

if (fhirEnvOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(fhirEnvOverrides);
}

builder.Services.AddOptions<OpenRouterOptions>()
    .Bind(builder.Configuration.GetSection(OpenRouterOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<LmStudioOptions>()
    .Bind(builder.Configuration.GetSection(LmStudioOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<LlmProviderOptions>()
    .Bind(builder.Configuration.GetSection(LlmProviderOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<FhirMappingOptions>()
    .Bind(builder.Configuration.GetSection(FhirMappingOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<RecordingOptions>()
    .Bind(builder.Configuration.GetSection(RecordingOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<FhirServerOptions>()
    .Bind(builder.Configuration.GetSection(FhirServerOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = builder.Environment.IsDevelopment(),
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
});

builder.Services.AddDbContext<ConcernsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ConcernsDb")
        ?? "Data Source=concerns.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddSingleton<IAiUsageTelemetry, AiUsageTelemetry>();
builder.Services.AddScoped<IConcernRepository, ConcernRepository>();
builder.Services.AddScoped<IAudioProcessingService, AudioProcessingService>();
builder.Services.AddScoped<IAudioTranscriptionService, OpenRouterAudioTranscriptionService>();
builder.Services.AddScoped<ITextCompletionClient, TextCompletionClient>();
builder.Services.AddScoped<IConcernExtractionService, ConcernExtractionService>();
builder.Services.AddScoped<IFollowUpQuestionService, FollowUpQuestionService>();
builder.Services.AddScoped<IConcernRefinementService, ConcernRefinementService>();
builder.Services.AddScoped<IFhirObservationMapper, FhirObservationMapper>();
builder.Services.AddScoped<IFhirBundleBuilder, FhirBundleBuilder>();
builder.Services.AddScoped<IFhirServerClient, FhirServerClient>();

builder.Services.AddHttpClient("OpenRouter", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenRouterOptions>>().Value;
    var baseUrl = EnsureTrailingSlash(options.BaseUrl);
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    if (!string.IsNullOrWhiteSpace(options.AppUrl))
    {
        client.DefaultRequestHeaders.Add("HTTP-Referer", options.AppUrl);
    }

    if (!string.IsNullOrWhiteSpace(options.AppName))
    {
        client.DefaultRequestHeaders.Add("X-Title", options.AppName);
    }
});

builder.Services.AddHttpClient("LmStudio", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LmStudioOptions>>().Value;
    var baseUrl = EnsureTrailingSlash(options.BaseUrl);
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient("FhirServer", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FhirServerOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        var baseUrl = EnsureTrailingSlash(options.BaseUrl);
        client.BaseAddress = new Uri(baseUrl);
    }
    else
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("FhirServer");
        logger.LogWarning("FHIR server base URL is not configured; FHIR uploads will be disabled.");
    }
    client.Timeout = TimeSpan.FromMinutes(2);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return TypedResults.Ok(new { token = tokens.RequestToken });
    })
    .Produces(StatusCodes.Status200OK);

app.MapPost("/api/audio/process", ProcessAudioAsync)
    .Produces<ProcessingResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

static string EnsureTrailingSlash(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "/";
    }

    return value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
}

static async Task<IResult> ProcessAudioAsync(
    HttpContext context,
    IAntiforgery antiforgery,
    IAudioProcessingService audioProcessingService,
    JsonSerializerOptions jsonOptions,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
        var request = context.Request;
        if (!request.HasFormContentType)
        {
            return TypedResults.Problem(
                "Expected multipart/form-data content.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        var file = form.Files.GetFile("audio");
        if (file is null || file.Length == 0)
        {
            return TypedResults.Problem(
                "Audio file is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        if (memoryStream.Length == 0)
        {
            return TypedResults.Problem(
                "Audio file is empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var subjectReference = form.TryGetValue("subjectReference", out var subjectReferenceValue)
            ? subjectReferenceValue.ToString()
            : null;
        var subjectDisplay = form.TryGetValue("subjectDisplay", out var subjectDisplayValue)
            ? subjectDisplayValue.ToString()
            : null;

        var processingContext = new ProcessingContext(
            string.IsNullOrWhiteSpace(subjectReference) ? null : subjectReference,
            string.IsNullOrWhiteSpace(subjectDisplay) ? null : subjectDisplay);

        var audioInput = new AudioInput(
            Convert.ToBase64String(memoryStream.ToArray()),
            string.IsNullOrWhiteSpace(file.ContentType) ? "audio/wav" : file.ContentType);

        var processingResult = await audioProcessingService
            .ProcessAsync(audioInput, processingContext, cancellationToken)
            .ConfigureAwait(false);

        var bundleJson = JsonSerializer.Serialize(processingResult.Bundle, jsonOptions);
        var response = new ProcessingResponse(processingResult.Transcript, processingResult.Concerns, bundleJson);

        return TypedResults.Ok(response);
    }
    catch (OperationCanceledException)
    {
        return TypedResults.Problem(
            "Audio processing canceled.",
            statusCode: StatusCodes.Status408RequestTimeout);
    }
    catch (AntiforgeryValidationException ex)
    {
        logger.LogWarning(ex, "Antiforgery validation failed.");
        return TypedResults.Problem(
            "Invalid antiforgery token.",
            statusCode: StatusCodes.Status400BadRequest);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Audio processing failed.");
        return TypedResults.Problem(
            "Audio processing failed. Check logs for details.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ConcernsDbContext>();
    await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
}

app.Run();
