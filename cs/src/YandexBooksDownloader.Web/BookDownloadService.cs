using System.Net;
using System.Text.RegularExpressions;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Web;

public sealed partial class BookDownloadService(
    SessionStore session,
    ILoggerFactory loggerFactory,
    IWebHostEnvironment environment)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public string OutDir => Path.Combine(environment.ContentRootPath, "out");

    public static bool IsValidBookId(string bookId) => MyRegex().IsMatch(bookId);

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

    public string? GetEpubPath(string bookId)
    {
        if (!IsValidBookId(bookId))
        {
            return null;
        }

        var path = Path.Combine(OutDir, bookId + ".epub");
        return File.Exists(path) ? path : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex MyRegex();
}
