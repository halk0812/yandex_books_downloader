using System.Security.Cryptography;

namespace YandexBooksDownloader.Core;

public static class AesCbc
{
    public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> cipherText)
    {
        if (key.Length is not (16 or 24 or 32))
        {
            throw new ArgumentException($"Invalid AES key length {key.Length}.", nameof(key));
        }

        if (iv.Length != 16)
        {
            throw new ArgumentException($"IV must be 16 bytes, got {iv.Length}.", nameof(iv));
        }

        if (cipherText.Length == 0 || cipherText.Length % 16 != 0)
        {
            throw new ArgumentException(
                $"Cipher text length must be a positive multiple of 16, got {cipherText.Length}.",
                nameof(cipherText));
        }

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        using var decryptor = aes.CreateDecryptor(key.ToArray(), iv.ToArray());
        return decryptor.TransformFinalBlock(cipherText.ToArray(), 0, cipherText.Length);
    }
}
