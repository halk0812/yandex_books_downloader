namespace YandexBooksDownloader.Core;

public static class SessionCookie
{
    public const string Name = "Session_id";
    public const string EnvironmentVariable = "SESSION_ID";

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
