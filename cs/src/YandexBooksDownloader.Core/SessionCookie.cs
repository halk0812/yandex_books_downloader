namespace YandexBooksDownloader.Core;

/// <summary>
/// Извлекает сессионную cookie Яндекса.
/// </summary>
public static class SessionCookie
{
    /// <summary>
    /// Имя cookie.
    /// </summary>
    public const string Name = "Session_id";

    /// <summary>
    /// Переменная окружения со значением cookie.
    /// </summary>
    public const string EnvironmentVariable = "SESSION_ID";

    /// <summary>
    /// Возвращает cookie из переменной окружения, иначе — результат запроса.
    /// </summary>
    /// <param name="prompt">Колбэк, используемый когда переменная окружения отсутствует или пустая.</param>
    /// <returns>Значение cookie или null, если его не удалось получить.</returns>
    public static string? Resolve(Func<string?> prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        return prompt();
    }
}
