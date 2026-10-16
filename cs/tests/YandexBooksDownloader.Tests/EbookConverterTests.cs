using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public class EbookConverterTests
{
    [Fact]
    public void BuildArguments_QuotesInputAndOutputPaths()
    {
        // Act
        var arguments = EbookConverter.BuildArguments(@"C:\out\KFHDG3bp.epub", @"C:\out\KFHDG3bp.mobi");

        // Assert
        arguments.Should().Be("\"C:\\out\\KFHDG3bp.epub\" \"C:\\out\\KFHDG3bp.mobi\"");
    }
}
