namespace AudioSharp.App.Data;

public sealed class ConcernRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Transcript { get; set; } = string.Empty;
    public string ConcernsJson { get; set; } = string.Empty;
    public string FhirBundleJson { get; set; } = string.Empty;
    public string? SubjectReference { get; set; }
    public string? SubjectDisplay { get; set; }
}
