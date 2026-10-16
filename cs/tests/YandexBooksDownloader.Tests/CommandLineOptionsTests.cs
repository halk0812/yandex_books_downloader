using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void Parse_AppliesDefaults()
    {
        // Act
        var options = CommandLineOptions.Parse(["--bookid", "KFHDG3bp"]);

        // Assert
        options.BookId.Should().Be("KFHDG3bp");
        options.OutDir.Should().Be("out");
        options.LogLevel.Should().Be("INFO");
        options.Download.Should().BeTrue();
        options.DeleteCss.Should().BeTrue();
        options.MakeEpub.Should().BeTrue();
        options.DeleteDownloaded.Should().BeTrue();
        options.ConvertMobi.Should().BeFalse();
        options.ConvertPdf.Should().BeFalse();
    }

    [Fact]
    public void Parse_SupportsEqualsAndSpaceValueForms()
    {
        // Act
        var options = CommandLineOptions.Parse(
        [
            "--bookid=KFHDG3bp",
            "--outdir", "out",
            "--log", "debug",
            "--download=false",
            "--delete_css", "false",
            "--make_epub=true",
            "--delete_downloaded", "False",
        ]);

        // Assert
        options.BookId.Should().Be("KFHDG3bp");
        options.OutDir.Should().Be("out");
        options.LogLevel.Should().Be("DEBUG");
        options.Download.Should().BeFalse();
        options.DeleteCss.Should().BeFalse();
        options.MakeEpub.Should().BeTrue();
        options.DeleteDownloaded.Should().BeFalse();
    }

    [Fact]
    public void Parse_SupportsMobiAndPdfFlags()
    {
        // Act
        var options = CommandLineOptions.Parse(["--bookid", "x", "--mobi", "true", "--pdf=true"]);

        // Assert
        options.ConvertMobi.Should().BeTrue();
        options.ConvertPdf.Should().BeTrue();
    }

    [Fact]
    public void Parse_ThrowsWithoutBookId()
    {
        // Act
        Action act = () => CommandLineOptions.Parse(["--outdir", "out"]);

        // Assert
        act.Should().Throw<CommandLineParseException>();
    }

    [Fact]
    public void Parse_ThrowsOnUnknownOption()
    {
        // Act
        Action act = () => CommandLineOptions.Parse(["--bookid", "x", "--nope"]);

        // Assert
        act.Should().Throw<CommandLineParseException>();
    }

    [Fact]
    public void Parse_ThrowsOnInvalidLogLevel()
    {
        // Act
        Action act = () => CommandLineOptions.Parse(["--bookid", "x", "--log", "VERBOSE"]);

        // Assert
        act.Should().Throw<CommandLineParseException>();
    }

    [Fact]
    public void Parse_ThrowsOnNonBoolFlagValue()
    {
        // Act
        Action act = () => CommandLineOptions.Parse(["--bookid", "x", "--download", "yes"]);

        // Assert
        act.Should().Throw<CommandLineParseException>();
    }

    [Fact]
    public void Parse_ThrowsOnMissingValue()
    {
        // Act
        Action act = () => CommandLineOptions.Parse(["--bookid"]);

        // Assert
        act.Should().Throw<CommandLineParseException>();
    }
}
