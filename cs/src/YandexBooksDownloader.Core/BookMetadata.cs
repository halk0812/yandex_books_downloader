using System.Text.Json;

namespace YandexBooksDownloader.Core;

public sealed class BookMetadata
{
    private readonly Dictionary<string, byte[]> _blobs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _values = new(StringComparer.Ordinal);

    private BookMetadata()
    {
    }

    public static BookMetadata FromJson(string json, string secretBase64)
    {
        using var document = JsonDocument.Parse(json);
        var metadata = new BookMetadata();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var bytes = ToBytes(property.Value);
                metadata._blobs[property.Name] = MetadataCipher.Decrypt(secretBase64, bytes);
            }
            else
            {
                metadata._values[property.Name] = property.Value.Clone();
            }
        }

        return metadata;
    }

    public byte[] GetBytes(string key)
    {
        return _blobs.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Metadata blob '{key}' is missing.");
    }

    public string GetString(string key)
    {
        if (!_values.TryGetValue(key, out var element))
        {
            throw new KeyNotFoundException($"Metadata value '{key}' is missing.");
        }

        return element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? throw new InvalidOperationException($"Metadata value '{key}' is null.")
            : element.ToString();
    }

    private static byte[] ToBytes(JsonElement array)
    {
        var bytes = new List<byte>(array.GetArrayLength());
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number ||
                !item.TryGetInt32(out var value) ||
                value is < 0 or > 255)
            {
                throw new InvalidDataException($"Expected byte value in metadata array, got '{item}'.");
            }

            bytes.Add((byte)value);
        }

        return [.. bytes];
    }
}
