using System.Text;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public class BookMetadataTests
{
    private const string SecretBase64 = "MDEyMzQ1Njc4OWFiY2RlZg==";

    [Fact]
    public void FromJson_DecryptsArrayValuesAndKeepsScalars()
    {
        // Arrange
        var opfPlain = Encoding.UTF8.GetBytes("<package>opf</package>");
        var ncxPlain = Encoding.UTF8.GetBytes("<ncx>toc</ncx>");
        var opfBytes = CryptoHelper.EncryptWithIv(SecretBase64, opfPlain);
        var ncxBytes = CryptoHelper.EncryptWithIv(SecretBase64, ncxPlain);

        var json = $$"""
            {
              "document_uuid": "uuid-123",
              "version": 4,
              "opf": [{{string.Join(",", opfBytes)}}],
              "ncx": [{{string.Join(",", ncxBytes)}}]
            }
            """;

        // Act
        var metadata = BookMetadata.FromJson(json, SecretBase64);

        // Assert
        metadata.GetBytes("opf").Should().Equal(opfPlain);
        metadata.GetBytes("ncx").Should().Equal(ncxPlain);
        metadata.GetString("document_uuid").Should().Be("uuid-123");
        metadata.GetString("version").Should().Be("4");
    }

    [Fact]
    public void GetBytes_ThrowsForMissingKey()
    {
        // Arrange
        var metadata = BookMetadata.FromJson("{}", SecretBase64);

        // Act
        Action act = () => metadata.GetBytes("opf");

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void FromJson_ThrowsOnNonByteArrayValues()
    {
        // Act
        Action act = () => BookMetadata.FromJson("""{"opf": [300]}""", SecretBase64);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }
}
