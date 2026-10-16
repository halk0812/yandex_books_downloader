namespace YandexBooksDownloader.Web;

/// <summary>
/// Хранит сессионную cookie Яндекса текущей браузерной сессии.
/// </summary>
public sealed class SessionStore
{
    /// <summary>
    /// Значение сессионной cookie, null когда вход не выполнен.
    /// </summary>
    public string? Cookie { get; set; }
}
