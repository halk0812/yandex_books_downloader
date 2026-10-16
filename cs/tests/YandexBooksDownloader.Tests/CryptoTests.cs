using System.Security.Cryptography;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
public sealed class CryptoTests
{
    private const string SecretBase64 = "MDEyMzQ1Njc4OWFiY2RlZg=="; // "0123456789abcdef"

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

    [TestMethod]
    public void Decrypt_RoundTripsPkcs7PaddedData()
    {
        var plain = System.Text.Encoding.UTF8.GetBytes("<html>hello world</html>");
        var data = EncryptWithIv(SecretBase64, plain);

        var decrypted = MetadataCipher.Decrypt(SecretBase64, data);

        CollectionAssert.AreEqual(plain, decrypted);
    }

    [TestMethod]
    public void Decrypt_RoundTripsBlockSizedData()
    {
        var plain = new byte[32];
        RandomNumberGenerator.Fill(plain);
        var data = EncryptWithIv(SecretBase64, plain);

        var decrypted = MetadataCipher.Decrypt(SecretBase64, data);

        CollectionAssert.AreEqual(plain, decrypted);
    }

    [TestMethod]
    public void Decrypt_ThrowsOnInvalidPadding()
    {
        var data = EncryptWithIv(SecretBase64, new byte[10]);
        data[^1] = 0x42;

        Assert.Throws<InvalidDataException>(() => MetadataCipher.Decrypt(SecretBase64, data));
    }

    [TestMethod]
    public void Decrypt_ThrowsOnShortData()
    {
        Assert.Throws<ArgumentException>(() => MetadataCipher.Decrypt(SecretBase64, new byte[16]));
    }

    [TestMethod]
    public void AesCbc_ThrowsOnMisalignedCipherText()
    {
        Assert.Throws<ArgumentException>(() => AesCbc.Decrypt(new byte[16], new byte[16], new byte[20]));
    }

    [TestMethod]
    public void AesCbc_ThrowsOnInvalidKeyLength()
    {
        Assert.Throws<ArgumentException>(() => AesCbc.Decrypt(new byte[15], new byte[16], new byte[16]));
    }
}
