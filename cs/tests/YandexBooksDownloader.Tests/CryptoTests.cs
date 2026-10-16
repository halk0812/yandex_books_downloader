using System.Security.Cryptography;
using System.Text;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public class CryptoTests
{
    private const string SecretBase64 = "MDEyMzQ1Njc4OWFiY2RlZg=="; // "0123456789abcdef"

    [Fact]
    public void Decrypt_RoundTripsPkcs7PaddedData()
    {
        // Arrange
        var plain = Encoding.UTF8.GetBytes("<html>hello world</html>");
        var data = CryptoHelper.EncryptWithIv(SecretBase64, plain);

        // Act
        var decrypted = MetadataCipher.Decrypt(SecretBase64, data);

        // Assert
        decrypted.Should().Equal(plain);
    }

    [Fact]
    public void Decrypt_RoundTripsBlockSizedData()
    {
        // Arrange
        var plain = new byte[32];
        RandomNumberGenerator.Fill(plain);
        var data = CryptoHelper.EncryptWithIv(SecretBase64, plain);

        // Act
        var decrypted = MetadataCipher.Decrypt(SecretBase64, data);

        // Assert
        decrypted.Should().Equal(plain);
    }

    [Fact]
    public void Decrypt_ThrowsOnInvalidPadding()
    {
        // Arrange
        var data = CryptoHelper.EncryptWithIv(SecretBase64, new byte[10]);
        data[^1] = 0x42;

        // Act
        Action act = () => MetadataCipher.Decrypt(SecretBase64, data);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Decrypt_ThrowsOnShortData()
    {
        // Act
        Action act = () => MetadataCipher.Decrypt(SecretBase64, new byte[16]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AesCbc_ThrowsOnMisalignedCipherText()
    {
        // Act
        Action act = () => AesCbc.Decrypt(new byte[16], new byte[16], new byte[20]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AesCbc_ThrowsOnInvalidKeyLength()
    {
        // Act
        Action act = () => AesCbc.Decrypt(new byte[15], new byte[16], new byte[16]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
