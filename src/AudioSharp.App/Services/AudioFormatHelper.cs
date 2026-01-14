namespace AudioSharp.App.Services;

public static class AudioFormatHelper
{
    public static string ResolveFormat(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "wav";
        }

        var parts = contentType.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "wav";
        }

        var normalized = parts[0]
            .Trim()
            .ToLowerInvariant();

        return normalized switch
        {
            "audio/wav" => "wav",
            "audio/x-wav" => "wav",
            "audio/mpeg" => "mp3",
            "audio/mp3" => "mp3",
            "audio/webm" => "webm",
            "audio/ogg" => "ogg",
            _ => "wav"
        };
    }
}
