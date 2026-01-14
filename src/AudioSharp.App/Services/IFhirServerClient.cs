using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IFhirServerClient
{
    Task<FhirUploadResult> UploadBundleAsync(string bundleJson, CancellationToken cancellationToken);
}
