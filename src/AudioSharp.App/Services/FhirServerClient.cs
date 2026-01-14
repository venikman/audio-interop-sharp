using System.Net.Http.Headers;
using System.Text;
using AudioSharp.App.Models;
using AudioSharp.App.Options;
using Microsoft.Extensions.Options;

namespace AudioSharp.App.Services;

public sealed class FhirServerClient : IFhirServerClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FhirServerOptions _options;
    private readonly ILogger<FhirServerClient> _logger;

    public FhirServerClient(
        IHttpClientFactory httpClientFactory,
        IOptions<FhirServerOptions> options,
        ILogger<FhirServerClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FhirUploadResult> UploadBundleAsync(string bundleJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("FHIR server base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(bundleJson))
        {
            throw new ArgumentException("FHIR bundle JSON is required.", nameof(bundleJson));
        }

        var client = _httpClientFactory.CreateClient("FhirServer");
        var requestUri = string.IsNullOrWhiteSpace(_options.BundleEndpoint)
            ? "Bundle"
            : _options.BundleEndpoint.TrimStart('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(bundleJson, Encoding.UTF8, "application/fhir+json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));

        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);
        }

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var location = response.Headers.Location?.ToString();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "FHIR upload failed: {StatusCode} {ReasonPhrase}.",
                (int)response.StatusCode,
                response.ReasonPhrase);
        }

        return new FhirUploadResult(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            location,
            responseBody);
    }

}
