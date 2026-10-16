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
    private static readonly string[] AllowedExtensions = ["epub", "mobi", "pdf"];

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
    /// Скачивает книгу, упаковывает её в epub, при необходимости конвертирует и возвращает результат.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <param name="convertMobi">Признак конвертации epub в mobi.</param>
    /// <param name="convertPdf">Признак конвертации epub в pdf.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат скачивания с предупреждениями конвертации.</returns>
    public async Task<DownloadResult> DownloadAsync(
        string bookId,
        bool convertMobi,
        bool convertPdf,
        CancellationToken cancellationToken)
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

            var warnings = new List<string>();
            if (convertMobi)
            {
                await ConvertAsync(book, "mobi", warnings, cancellationToken);
            }

            if (convertPdf)
            {
                await ConvertAsync(book, "pdf", warnings, cancellationToken);
            }

            return new DownloadResult(bookId, warnings);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Возвращает путь файла книги в указанном формате, если он существует.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <param name="extension">Расширение файла без точки: epub, mobi или pdf.</param>
    /// <returns>Путь к файлу или null, если файл отсутствует.</returns>
    public string? FindBookPath(string bookId, string extension)
    {
        if (!IsValidBookId(bookId) || !AllowedExtensions.Contains(extension))
        {
            return null;
        }

        var path = Path.Combine(OutDir, bookId + "." + extension);
        return File.Exists(path) ? path : null;
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private static async Task ConvertAsync(
        BookDownloader book,
        string format,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var targetPath = Path.ChangeExtension(book.EpubPath, "." + format);
        try
        {
            await EbookConverter.ConvertAsync(book.EpubPath, targetPath, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            warnings.Add(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            warnings.Add($"cannot convert to {format}: {exception.Message}");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex MyRegex();
}
