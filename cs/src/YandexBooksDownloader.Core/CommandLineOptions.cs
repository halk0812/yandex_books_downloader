namespace YandexBooksDownloader.Core;

public sealed record CommandLineOptions
{
    public required string BookId { get; init; }
    public string OutDir { get; init; } = "out";
    public string LogLevel { get; init; } = "INFO";
    public bool Download { get; init; } = true;
    public bool DeleteCss { get; init; } = true;
    public bool MakeEpub { get; init; } = true;
    public bool DeleteDownloaded { get; init; } = true;

    public static bool IsHelp(string arg) => arg is "-h" or "--help";

    public static CommandLineOptions Parse(string[] args)
    {
        string? bookId = null;
        string? outDir = null;
        string? logLevel = null;
        bool? download = null;
        bool? deleteCss = null;
        bool? makeEpub = null;
        bool? deleteDownloaded = null;

        for (var i = 0; i < args.Length; i++)
        {
            var (name, inlineValue) = SplitOption(args[i]);
            switch (name)
            {
                case "--bookid":
                    bookId = TakeValue(args, ref i, name, inlineValue);
                    break;
                case "--outdir":
                    outDir = TakeValue(args, ref i, name, inlineValue);
                    break;
                case "--log":
                    logLevel = TakeValue(args, ref i, name, inlineValue);
                    break;
                case "--download":
                    download = TakeBool(args, ref i, name, inlineValue);
                    break;
                case "--delete_css":
                    deleteCss = TakeBool(args, ref i, name, inlineValue);
                    break;
                case "--make_epub":
                    makeEpub = TakeBool(args, ref i, name, inlineValue);
                    break;
                case "--delete_downloaded":
                    deleteDownloaded = TakeBool(args, ref i, name, inlineValue);
                    break;
                default:
                    throw new CommandLineParseException($"Unknown option '{args[i]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(bookId))
        {
            throw new CommandLineParseException("Option '--bookid' is required.");
        }

        if (logLevel is not null && !ValidLogLevels.Contains(logLevel.ToUpperInvariant()))
        {
            throw new CommandLineParseException(
                $"Invalid --log value '{logLevel}'. Choose from: {string.Join(", ", ValidLogLevels)}.");
        }

        return new CommandLineOptions
        {
            BookId = bookId,
            OutDir = outDir ?? "out",
            LogLevel = (logLevel ?? "INFO").ToUpperInvariant(),
            Download = download ?? true,
            DeleteCss = deleteCss ?? true,
            MakeEpub = makeEpub ?? true,
            DeleteDownloaded = deleteDownloaded ?? true,
        };
    }

    private static readonly string[] ValidLogLevels = ["TRACE", "DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"];

    public static string HelpText { get; } = $"""
        usage: yandex-books-downloader --bookid <BookId> [options]

        Downloads books from https://{BookDownloader.BooksDomain} and saves them as epub files.

        options:
          --bookid <id>               bookid, take from the book url (required)
          --outdir <dir>              output directory (default: "out")
          --log <level>               log level: {string.Join(", ", ValidLogLevels)} (default: INFO)
          --download <true|false>     download book files (default: true)
          --delete_css <true|false>   empty css files before packing (default: true)
          --make_epub <true|false>    pack downloaded files into an epub (default: true)
          --delete_downloaded <b>     remove the unpacked directory after packing (default: true)
          -h, --help                  show this help

        Cookie: set the SESSION_ID environment variable, or the program will prompt
        for the {SessionCookie.Name} cookie of https://{BookDownloader.BooksDomain}.
        """;

    private static (string Name, string? Value) SplitOption(string arg)
    {
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            throw new CommandLineParseException($"Unexpected argument '{arg}'.");
        }

        var index = arg.IndexOf('=');
        return index < 0 ? (arg, null) : (arg[..index], arg[(index + 1)..]);
    }

    private static string TakeValue(string[] args, ref int i, string name, string? inlineValue)
    {
        if (inlineValue is not null)
        {
            return inlineValue;
        }

        if (i + 1 >= args.Length)
        {
            throw new CommandLineParseException($"Option '{name}' requires a value.");
        }

        return args[++i];
    }

    private static bool TakeBool(string[] args, ref int i, string name, string? inlineValue)
    {
        var value = TakeValue(args, ref i, name, inlineValue);
        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        throw new CommandLineParseException($"Option '{name}' expects true or false, got '{value}'.");
    }
}

public sealed class CommandLineParseException(string message) : Exception(message);
