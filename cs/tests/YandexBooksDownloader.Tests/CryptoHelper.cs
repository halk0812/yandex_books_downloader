using System.Security.Cryptography;

namespace YandexBooksDownloader.Tests;

internal static class CryptoHelper
{
    internal static byte[] EncryptWithIv(string secretBase64, byte[] plainText)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Convert.FromBase64String(secretBase64);
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var cipherText = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
        return [.. aes.IV, .. cipherText];
    }
}
