namespace YandexBooksDownloader.Core;

/// <summary>
/// Расшифровывает значения метаданных Яндекс Книг.
/// </summary>
public static class MetadataCipher
{
    /// <summary>
    /// Расшифровывает значение, зашифрованное AES-CBC с дополнением PKCS7.
    /// </summary>
    /// <param name="secretBase64">Секрет расшифровки в Base64.</param>
    /// <param name="data">Вектор инициализации, за которым следует шифртекст.</param>
    /// <returns>Расшифрованные байты без дополнения.</returns>
    public static byte[] Decrypt(string secretBase64, ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(secretBase64);

        if (data.Length < 32)
        {
            throw new ArgumentException($"Encrypted data is too short ({data.Length} bytes).", nameof(data));
        }

        var key = Convert.FromBase64String(secretBase64);
        var iv = data[..16];
        var cipherText = data[16..];
        var plain = AesCbc.Decrypt(key, iv, cipherText);

        var pad = plain[^1];
        if (pad is < 1 or > 16 || pad > plain.Length)
        {
            throw new InvalidDataException($"Invalid PKCS7 padding byte: {pad}.");
        }

        return plain[..(plain.Length - pad)];
    }
}
