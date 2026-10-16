namespace YandexBooksDownloader.Web;

/// <summary>
/// Тело запроса на скачивание.
/// </summary>
/// <param name="BookId">Идентификатор книги.</param>
internal sealed record DownloadRequest(string? BookId);
