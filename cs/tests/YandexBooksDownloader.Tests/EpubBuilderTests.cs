using System.IO.Compression;
using System.Text;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
public sealed class EpubBuilderTests
{
    private string _sourceDir = string.Empty;
    private string _epubPath = string.Empty;

    [TestInitialize]
    public void Initialize()
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

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_sourceDir, recursive: true);
        File.Delete(_epubPath);
    }

    [TestMethod]
    public void Create_PutsStoredMimetypeFirst()
    {
        EpubBuilder.Create(_sourceDir, _epubPath);

        using var archive = ZipFile.OpenRead(_epubPath);
        Assert.AreEqual("mimetype", archive.Entries[0].FullName);
        Assert.AreEqual(archive.Entries[0].Length, archive.Entries[0].CompressedLength);

        using var mimeStream = archive.Entries[0].Open();
        using var reader = new StreamReader(mimeStream);
        Assert.AreEqual(EpubBuilder.MimeType, reader.ReadToEnd());
    }

    [TestMethod]
    public void Create_UsesForwardSlashesAndDeflatesOtherEntries()
    {
        EpubBuilder.Create(_sourceDir, _epubPath);

        using var archive = ZipFile.OpenRead(_epubPath);
        var names = archive.Entries.Select(e => e.FullName).ToList();
        List<string> expected = ["mimetype", "META-INF/container.xml", "OEBPS/text/part1.xhtml"];
        CollectionAssert.AreEquivalent(expected, names);

        var part = archive.GetEntry("OEBPS/text/part1.xhtml")!;
        Assert.IsGreaterThan(part.CompressedLength, part.Length);
    }
}
