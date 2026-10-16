using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public sealed class CssCleanerTests : IDisposable
{
    private readonly string _dir;

    public CssCleanerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "css-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_dir, "OEBPS", "style"));
        File.WriteAllText(Path.Combine(_dir, "OEBPS", "style", "style.css"), "body { color: red; }");
        File.WriteAllText(Path.Combine(_dir, "OEBPS", "style", "STYLE.CSS"), "body { color: blue; }");
        File.WriteAllText(Path.Combine(_dir, "OEBPS", "content.opf"), "<package/>");
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void ClearAll_EmptiesCssFilesRecursively()
    {
        // Act
        CssCleaner.ClearAll(_dir);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "OEBPS", "style", "style.css")).Should().BeEmpty();
        File.ReadAllText(Path.Combine(_dir, "OEBPS", "style", "STYLE.CSS")).Should().BeEmpty();
        File.ReadAllText(Path.Combine(_dir, "OEBPS", "content.opf")).Should().Be("<package/>");
    }
}
