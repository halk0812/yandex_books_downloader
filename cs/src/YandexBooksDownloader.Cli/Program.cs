using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Logging;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Cli;

/// <summary>
/// Точка входа консольного приложения.
/// </summary>
public static class Program
{
    /// <summary>
    /// Главный метод.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    /// <returns>Код завершения процесса.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Any(CommandLineOptions.IsHelp))
        {
            Console.WriteLine(CommandLineOptions.HelpText);
            return 0;
        }

        CommandLineOptions options;
        try
        {
            options = CommandLineOptions.Parse(args);
        }
        catch (CommandLineParseException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(CommandLineOptions.HelpText);
            return 2;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(MapLogLevel(options.LogLevel));
            builder.AddSimpleConsole(console =>
            {
                console.SingleLine = true;
                console.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });
        });
        var logger = loggerFactory.CreateLogger("yandex_books_downloader");

        var cookie = SessionCookie.Resolve(() =>
        {
            Console.WriteLine($"Enter {SessionCookie.Name} cookie");
            Console.WriteLine(
                $"(your browser -> developer tools -> application -> Cookies -> https://{BookDownloader.BooksDomain} -> {SessionCookie.Name} -> Value) :");
            return Console.ReadLine();
        });

        if (string.IsNullOrWhiteSpace(cookie))
        {
            logger.LogError("{CookieName} cookie is required.", SessionCookie.Name);
            return 1;
        }

        if (!Directory.Exists(options.OutDir))
        {
            logger.LogInformation("Creating folder {OutDir} ...", options.OutDir);
            Directory.CreateDirectory(options.OutDir);
        }

        using var handler = new SocketsHttpHandler { UseCookies = true };
        handler.CookieContainer.Add(new Cookie(SessionCookie.Name, cookie, "/", BookDownloader.BooksDomain));
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        var book = new YandexBook(options.OutDir, cookie, http, loggerFactory).GetBook(options.BookId);

        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            if (options.Download)
            {
                await book.DownloadAsync(cts.Token);
            }

            if (options.DeleteCss)
            {
                book.ClearCss();
            }

            if (options.MakeEpub)
            {
                book.MakeEpub();
            }

            if (options.DeleteDownloaded)
            {
                book.DeleteDownloaded();
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("cancelled.");
            return 130;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "fatal error");
            return 1;
        }
    }

    private static LogLevel MapLogLevel(string level) => level switch
    {
        "TRACE" => LogLevel.Trace,
        "DEBUG" => LogLevel.Debug,
        "INFO" => LogLevel.Information,
        "WARNING" => LogLevel.Warning,
        "ERROR" => LogLevel.Error,
        "CRITICAL" => LogLevel.Critical,
        _ => LogLevel.Information,
    };
}
