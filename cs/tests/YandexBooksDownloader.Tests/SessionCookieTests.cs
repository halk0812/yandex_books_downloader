using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

public sealed class SessionCookieTests : IDisposable
{
    public void Dispose() => Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, value: null);

    [Fact]
    public void Resolve_PrefersEnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, "  cookie-from-env  ");

        // Act
        var cookie = SessionCookie.Resolve(() => "prompted");

        // Assert
        cookie.Should().Be("cookie-from-env");
    }

    [Fact]
    public void Resolve_FallsBackToPromptWhenEnvMissing()
    {
        // Arrange
        Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, value: null);

        // Act
        var cookie = SessionCookie.Resolve(() => "cookie-from-prompt");

        // Assert
        cookie.Should().Be("cookie-from-prompt");
    }

    [Fact]
    public void Resolve_ReturnsPromptResultWhenEnvIsBlank()
    {
        // Arrange
        Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, "   ");

        // Act
        var cookie = SessionCookie.Resolve(() => null);

        // Assert
        cookie.Should().BeNull();
    }
}
