using System.Text;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
public sealed class BookMetadataTests
{
    private const string SecretBase64 = "MDEyMzQ1Njc4OWFiY2RlZg==";

    [TestMethod]
    public void FromJson_DecryptsArrayValuesAndKeepsScalars()
    {
        var opfPlain = Encoding.UTF8.GetBytes("<package>opf</package>");
        var ncxPlain = Encoding.UTF8.GetBytes("<ncx>toc</ncx>");
        var opfBytes = CryptoTests.EncryptWithIv(SecretBase64, opfPlain);
        var ncxBytes = CryptoTests.EncryptWithIv(SecretBase64, ncxPlain);

        var json = $$"""
            {
              "document_uuid": "uuid-123",
              "version": 4,
              "opf": [{{string.Join(",", opfBytes)}}],
              "ncx": [{{string.Join(",", ncxBytes)}}]
            }
            """;

        var metadata = BookMetadata.FromJson(json, SecretBase64);

        CollectionAssert.AreEqual(opfPlain, metadata.GetBytes("opf"));
        CollectionAssert.AreEqual(ncxPlain, metadata.GetBytes("ncx"));
        Assert.AreEqual("uuid-123", metadata.GetString("document_uuid"));
        Assert.AreEqual("4", metadata.GetString("version"));
    }

    [TestMethod]
    public void GetBytes_ThrowsForMissingKey()
    {
        Assert.Throws<KeyNotFoundException>(
            () => BookMetadata.FromJson("{}", SecretBase64).GetBytes("opf"));
    }

    [TestMethod]
    public void FromJson_ThrowsOnNonByteArrayValues()
    {
        Assert.Throws<InvalidDataException>(
            () => BookMetadata.FromJson("""{"opf": [300]}""", SecretBase64));
    }
}
