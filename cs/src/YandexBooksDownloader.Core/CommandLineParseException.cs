namespace YandexBooksDownloader.Core;

/// <summary>
/// Ошибка разбора аргументов командной строки.
/// </summary>
/// <param name="message">Сообщение об ошибке.</param>
public sealed class CommandLineParseException(string message) : Exception(message);
