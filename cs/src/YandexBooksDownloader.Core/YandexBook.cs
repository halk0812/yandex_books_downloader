using Microsoft.Extensions.Logging;

namespace YandexBooksDownloader.Core;

public sealed class YandexBook
{
    private readonly string _outDir;
    private readonly HttpClient _http;
    private readonly ILoggerFactory _loggerFactory;

    public YandexBook(string outDir, string sessionCookie, HttpClient http, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionCookie);
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _outDir = Path.GetFullPath(outDir);
        _http = http;
        _loggerFactory = loggerFactory;
    }

    public BookDownloader GetBook(string bookId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookId);

        var bookDir = Path.Combine(_outDir, bookId);
        Directory.CreateDirectory(bookDir);
        var logger = _loggerFactory.CreateLogger<Downloader>();
        var downloader = new Downloader(_http, bookDir, logger);
        return new BookDownloader(bookId, downloader, _loggerFactory.CreateLogger<BookDownloader>());
    }
}
