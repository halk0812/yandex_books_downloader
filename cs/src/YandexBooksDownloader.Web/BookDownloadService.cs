using System.Net;
using System.Text.RegularExpressions;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Web;

/// <summary>
/// Скачивает книги, запрошенные через web api.
/// </summary>
/// <param name="session">Хранилище сессии с cookie входа.</param>
/// <param name="loggerFactory">Фабрика логгеров.</param>
/// <param name="environment">Окружение web-хоста.</param>
public sealed partial class BookDownloadService(
    SessionStore session,
    ILoggerFactory loggerFactory,
    IWebHostEnvironment environment) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Каталог вывода скачанных книг.
    /// </summary>
    public string OutDir => Path.Combine(environment.ContentRootPath, "out");

    /// <summary>
    /// Проверяет безопасный формат идентификатора книги.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <returns>Истина, если идентификатор безопасен.</returns>
    public static bool IsValidBookId(string bookId) => MyRegex().IsMatch(bookId);

    /// <summary>
    /// Скачивает книгу, упаковывает её в epub и возвращает идентификатор книги.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Идентификатор скачанной книги.</returns>
    public async Task<string> DownloadAsync(string bookId, CancellationToken cancellationToken)
    {
        if (session.Cookie is null)
        {
            throw new InvalidOperationException("Not logged in. Sign in with Yandex first.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(OutDir);

            using var handler = new SocketsHttpHandler { UseCookies = true };
            handler.CookieContainer.Add(new Cookie(SessionCookie.Name, session.Cookie, "/", BookDownloader.BooksDomain));
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

            var book = new YandexBook(OutDir, session.Cookie, http, loggerFactory).GetBook(bookId);
            await book.DownloadAsync(cancellationToken);
            book.ClearCss();
            book.MakeEpub();
            book.DeleteDownloaded();
            return bookId;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Возвращает путь epub-файла книги, если он существует.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <returns>Путь к epub-файлу или null, если книга ещё не скачана.</returns>
    public string? FindEpubPath(string bookId)
    {
        if (!IsValidBookId(bookId))
        {
            return null;
        }

        var path = Path.Combine(OutDir, bookId + ".epub");
        return File.Exists(path) ? path : null;
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex MyRegex();
}
