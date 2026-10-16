using System.Text;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
public sealed class CssCleanerTests
{
    private string _dir = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "css-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_dir, "OEBPS", "style"));
        File.WriteAllText(Path.Combine(_dir, "OEBPS", "style", "style.css"), "body { color: red; }");
        File.WriteAllText(Path.Combine(_dir, "OEBPS", "style", "STYLE.CSS"), "body { color: blue; }");
        File.WriteAllText(Path.Combine(_dir, "OEBPS", "content.opf"), "<package/>");
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_dir, recursive: true);

    [TestMethod]
    public void ClearAll_EmptiesCssFilesRecursively()
    {
        CssCleaner.ClearAll(_dir);

        Assert.AreEqual(string.Empty, File.ReadAllText(Path.Combine(_dir, "OEBPS", "style", "style.css")));
        Assert.AreEqual(string.Empty, File.ReadAllText(Path.Combine(_dir, "OEBPS", "style", "STYLE.CSS")));
        Assert.AreEqual("<package/>", File.ReadAllText(Path.Combine(_dir, "OEBPS", "content.opf")));
    }
}
