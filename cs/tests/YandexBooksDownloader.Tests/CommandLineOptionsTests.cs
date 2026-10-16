using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
public sealed class CommandLineOptionsTests
{
    [TestMethod]
    public void Parse_AppliesDefaults()
    {
        var options = CommandLineOptions.Parse(["--bookid", "KFHDG3bp"]);

        Assert.AreEqual("KFHDG3bp", options.BookId);
        Assert.AreEqual("out", options.OutDir);
        Assert.AreEqual("INFO", options.LogLevel);
        Assert.IsTrue(options.Download);
        Assert.IsTrue(options.DeleteCss);
        Assert.IsTrue(options.MakeEpub);
        Assert.IsTrue(options.DeleteDownloaded);
    }

    [TestMethod]
    public void Parse_SupportsEqualsAndSpaceValueForms()
    {
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

        Assert.AreEqual("KFHDG3bp", options.BookId);
        Assert.AreEqual("out", options.OutDir);
        Assert.AreEqual("DEBUG", options.LogLevel);
        Assert.IsFalse(options.Download);
        Assert.IsFalse(options.DeleteCss);
        Assert.IsTrue(options.MakeEpub);
        Assert.IsFalse(options.DeleteDownloaded);
    }

    [TestMethod]
    public void Parse_ThrowsWithoutBookId()
    {
        Assert.Throws<CommandLineParseException>(() => CommandLineOptions.Parse(["--outdir", "out"]));
    }

    [TestMethod]
    public void Parse_ThrowsOnUnknownOption()
    {
        Assert.Throws<CommandLineParseException>(() => CommandLineOptions.Parse(["--bookid", "x", "--nope"]));
    }

    [TestMethod]
    public void Parse_ThrowsOnInvalidLogLevel()
    {
        Assert.Throws<CommandLineParseException>(() => CommandLineOptions.Parse(["--bookid", "x", "--log", "VERBOSE"]));
    }

    [TestMethod]
    public void Parse_ThrowsOnNonBoolFlagValue()
    {
        Assert.Throws<CommandLineParseException>(() => CommandLineOptions.Parse(["--bookid", "x", "--download", "yes"]));
    }

    [TestMethod]
    public void Parse_ThrowsOnMissingValue()
    {
        Assert.Throws<CommandLineParseException>(() => CommandLineOptions.Parse(["--bookid"]));
    }
}
