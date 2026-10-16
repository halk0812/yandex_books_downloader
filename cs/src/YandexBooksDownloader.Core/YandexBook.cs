using Microsoft.Extensions.Logging;

namespace YandexBooksDownloader.Core;

/// <summary>
/// Точка входа для скачивания книг с общей сессионной cookie.
/// </summary>
public sealed class YandexBook
{
    private readonly string _outDir;
    private readonly HttpClient _http;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexBook"/> class.
    /// </summary>
    /// <param name="outDir">Корневой каталог вывода.</param>
    /// <param name="sessionCookie">Сессионная cookie Яндекса.</param>
    /// <param name="http">HTTP-клиент, несущий cookie.</param>
    /// <param name="loggerFactory">Фабрика логгеров.</param>
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

    /// <summary>
    /// Создаёт загрузчик книги в отдельном подкаталоге.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <returns>Загрузчик книги.</returns>
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
