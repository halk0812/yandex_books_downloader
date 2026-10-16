using System.Net;
using Microsoft.Extensions.Logging;
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
    var bookId = request?.BookId;
    if (string.IsNullOrWhiteSpace(bookId) || !BookDownloadService.IsValidBookId(bookId))
    {
        return Results.BadRequest(new { error = "bookid is required and may contain only letters, digits, '-' and '_'." });
    }

    try
    {
        var downloadedId = await downloads.DownloadAsync(bookId, ct);
        return Results.Ok(new { epubUrl = $"/api/epub/{downloadedId}" });
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

app.MapGet("/api/epub/{bookId}", (BookDownloadService downloads, string bookId) =>
{
    var path = downloads.GetEpubPath(bookId);
    return path is null
        ? Results.NotFound(new { error = "epub not found, download the book first." })
        : Results.File(path, "application/epub+zip", $"{bookId}.epub");
});

app.Run();

internal sealed record DownloadRequest(string? BookId);
