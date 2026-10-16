namespace YandexBooksDownloader.Web;

/// <summary>
/// Ошибка таймаута интерактивного входа.
/// </summary>
/// <param name="message">Сообщение об ошибке.</param>
public sealed class LoginTimeoutException(string message) : Exception(message);
