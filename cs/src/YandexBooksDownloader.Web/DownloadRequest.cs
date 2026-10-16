namespace YandexBooksDownloader.Web;

/// <summary>
/// Тело запроса на скачивание.
/// </summary>
/// <param name="BookId">Идентификатор книги.</param>
/// <param name="Mobi">Признак конвертации epub в mobi.</param>
/// <param name="Pdf">Признак конвертации epub в pdf.</param>
internal sealed record DownloadRequest(string? BookId, bool Mobi, bool Pdf);
