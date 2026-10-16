using System.IO.Compression;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public sealed class EpubBuilderTests : IDisposable
{
    private readonly string _sourceDir;
    private readonly string _epubPath;

    public EpubBuilderTests()
    {
        _sourceDir = Path.Combine(Path.GetTempPath(), "epub-src-" + Guid.NewGuid().ToString("N"));
        _epubPath = _sourceDir + ".epub";
        Directory.CreateDirectory(Path.Combine(_sourceDir, "META-INF"));
        Directory.CreateDirectory(Path.Combine(_sourceDir, "OEBPS", "text"));

        File.WriteAllText(Path.Combine(_sourceDir, "mimetype"), EpubBuilder.MimeType);
        File.WriteAllText(Path.Combine(_sourceDir, "META-INF", "container.xml"), "<container/>");
        File.WriteAllText(
            Path.Combine(_sourceDir, "OEBPS", "text", "part1.xhtml"),
            string.Concat(Enumerable.Repeat("<html>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</html>", 50)));
    }

    public void Dispose()
    {
        Directory.Delete(_sourceDir, recursive: true);
        File.Delete(_epubPath);
    }

    [Fact]
    public void Create_PutsStoredMimetypeFirst()
    {
        // Act
        EpubBuilder.Create(_sourceDir, _epubPath);

        // Assert
        using var archive = ZipFile.OpenRead(_epubPath);
        archive.Entries[0].FullName.Should().Be("mimetype");
        archive.Entries[0].Length.Should().Be(archive.Entries[0].CompressedLength);

        using var mimeStream = archive.Entries[0].Open();
        using var reader = new StreamReader(mimeStream);
        reader.ReadToEnd().Should().Be(EpubBuilder.MimeType);
    }

    [Fact]
    public void Create_UsesForwardSlashesAndDeflatesOtherEntries()
    {
        // Act
        EpubBuilder.Create(_sourceDir, _epubPath);

        // Assert
        using var archive = ZipFile.OpenRead(_epubPath);
        var names = archive.Entries.Select(e => e.FullName).ToList();
        List<string> expected = ["mimetype", "META-INF/container.xml", "OEBPS/text/part1.xhtml"];
        names.Should().BeEquivalentTo(expected);

        var part = archive.GetEntry("OEBPS/text/part1.xhtml")!;
        part.CompressedLength.Should().BeLessThan(part.Length);
    }
}
