namespace AudioSharp.App.Models;

public sealed record FhirUploadResult(
    bool Success,
    int StatusCode,
    string? Location,
    string? ResponseBody);
