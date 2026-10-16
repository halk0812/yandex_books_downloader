using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public class OpfItemsTests
{
    [Fact]
    public void GetItemHrefs_ReturnsHrefsInDocumentOrder()
    {
        // Arrange
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

        // Act
        var hrefs = OpfItems.GetItemHrefs(opf);

        // Assert
        List<string> expected = ["toc.ncx", "style/style.css", "text/part1.xhtml"];
        hrefs.Should().Equal(expected);
    }

    [Fact]
    public void GetItemHrefs_MatchesItemsInAnyNamespace()
    {
        // Arrange
        var opf = """
            <opf:package xmlns:opf="http://www.idpf.org/2007/opf">
              <opf:manifest>
                <opf:item href="text/part1.xhtml"/>
              </opf:manifest>
            </opf:package>
            """;

        // Act
        var hrefs = OpfItems.GetItemHrefs(opf);

        // Assert
        List<string> expected = ["text/part1.xhtml"];
        hrefs.Should().Equal(expected);
    }
}
