using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Tests;

[TestClass]
[DoNotParallelize]
public sealed class SessionCookieTests
{
    [TestCleanup]
    public void Cleanup() => Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, null);

    [TestMethod]
    public void Resolve_PrefersEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, "  cookie-from-env  ");

        var cookie = SessionCookie.Resolve(() => "prompted");

        Assert.AreEqual("cookie-from-env", cookie);
    }

    [TestMethod]
    public void Resolve_FallsBackToPromptWhenEnvMissing()
    {
        Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, null);

        var cookie = SessionCookie.Resolve(() => "cookie-from-prompt");

        Assert.AreEqual("cookie-from-prompt", cookie);
    }

    [TestMethod]
    public void Resolve_ReturnsPromptResultWhenEnvIsBlank()
    {
        Environment.SetEnvironmentVariable(SessionCookie.EnvironmentVariable, "   ");

        var cookie = SessionCookie.Resolve(() => null);

        Assert.IsNull(cookie);
    }
}
