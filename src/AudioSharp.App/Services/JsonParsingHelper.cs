using System.Text;
using System.Text.Json;

namespace AudioSharp.App.Services;

public static class JsonParsingHelper
{
    public static bool TryDeserializeJson<T>(string raw, JsonSerializerOptions options, out T? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var json = ExtractJsonObject(raw);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, options);
            return result is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return string.Empty;
        }

        return raw[start..(end + 1)];
    }
}
