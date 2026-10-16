using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
public sealed class OpfItemsTests
{
    [TestMethod]
    public void GetItemHrefs_ReturnsHrefsInDocumentOrder()
    {
        var opf = """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="2.0">
              <manifest>
                <item id="ncx" href="toc.ncx" media-type="application/x-dtbncx+xml"/>
                <item id="css" href="style/style.css" media-type="text/css"/>
                <item id="c1" href="text/part1.xhtml" media-type="application/xhtml+xml"/>
                <item id="broken" media-type="text/plain"/>
              </manifest>
            </package>
            """;

        var hrefs = OpfItems.GetItemHrefs(opf);
        List<string> expected = ["toc.ncx", "style/style.css", "text/part1.xhtml"];

        CollectionAssert.AreEqual(expected, hrefs.ToList());
    }

    [TestMethod]
    public void GetItemHrefs_MatchesItemsInAnyNamespace()
    {
        var opf = """
            <opf:package xmlns:opf="http://www.idpf.org/2007/opf">
              <opf:manifest>
                <opf:item href="text/part1.xhtml"/>
              </opf:manifest>
            </opf:package>
            """;

        List<string> expected = ["text/part1.xhtml"];
        CollectionAssert.AreEqual(expected, OpfItems.GetItemHrefs(opf).ToList());
    }
}
