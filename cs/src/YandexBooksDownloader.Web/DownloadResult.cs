namespace YandexBooksDownloader.Web;

/// <summary>
/// Результат скачивания книги.
/// </summary>
/// <param name="BookId">Идентификатор скачанной книги.</param>
/// <param name="Warnings">Предупреждения, полученные при конвертации.</param>
public sealed record DownloadResult(string BookId, IReadOnlyList<string> Warnings);
