using System.Net;
using YandexBooksDownloader.Core;
using YandexBooksDownloader.Web;

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Directory.Exists(webRoot) ? webRoot : null,
});

builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<YandexLoginService>();
builder.Services.AddSingleton<BookDownloadService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/session", (SessionStore session) =>
    Results.Ok(new { loggedIn = session.Cookie is not null }));

app.MapGet("/api/capabilities", () =>
    Results.Ok(new { calibre = EbookConverter.IsConverterAvailable() }));

app.MapPost("/api/login", async (YandexLoginService login, CancellationToken ct) =>
{
    try
    {
        await login.LoginAsync(ct);
        return Results.Ok(new { loggedIn = true });
    }
    catch (LoginTimeoutException exception)
    {
        return Results.Json(new { error = exception.Message }, statusCode: (int)HttpStatusCode.RequestTimeout);
    }
});

app.MapPost("/api/logout", (SessionStore session) =>
{
    session.Cookie = null;
    return Results.Ok(new { loggedIn = false });
});

app.MapPost("/api/download", async (BookDownloadService downloads, DownloadRequest request, CancellationToken ct) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.BookId) || !BookDownloadService.IsValidBookId(request.BookId))
    {
        return Results.BadRequest(new { error = "bookid is required and may contain only letters, digits, '-' and '_'." });
    }

    try
    {
        var result = await downloads.DownloadAsync(request.BookId, request.Mobi, request.Pdf, ct);

        var formats = new Dictionary<string, string>();
        foreach (var format in new[] { "epub", "mobi", "pdf" })
        {
            if (downloads.FindBookPath(result.BookId, format) is not null)
            {
                formats[format] = $"/api/book/{result.BookId}/{format}";
            }
        }

        return Results.Ok(new { formats, warnings = result.Warnings });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Json(new { error = exception.Message }, statusCode: (int)HttpStatusCode.Unauthorized);
    }
    catch (HttpRequestException exception)
    {
        return Results.Json(new { error = exception.Message }, statusCode: (int)HttpStatusCode.BadGateway);
    }
});

app.MapGet("/api/book/{bookId}/{format}", (BookDownloadService downloads, string bookId, string format) =>
{
    var path = downloads.FindBookPath(bookId, format);
    var contentType = FindContentType(format);
    return path is null || contentType is null
        ? Results.NotFound(new { error = "file not found, download the book first." })
        : Results.File(path, contentType, $"{bookId}.{format}");
});

app.Run();

static string? FindContentType(string format) => format switch
{
    "epub" => "application/epub+zip",
    "mobi" => "application/x-mobipocket-ebook",
    "pdf" => "application/pdf",
    _ => null,
};
