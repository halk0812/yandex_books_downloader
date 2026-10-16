using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Playwright;
using YandexBooksDownloader.Core;

namespace YandexBooksDownloader.Web;

/// <summary>
/// Получает сессионную cookie Яндекса, открывая интерактивный вход в браузере.
/// </summary>
/// <param name="store">Хранилище сессии для сохранения полученной cookie.</param>
/// <param name="logger">Логгер.</param>
public sealed class YandexLoginService(SessionStore store, ILogger<YandexLoginService> logger)
{
    private static readonly string UserDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YandexBooksDownloader",
        "browser-profile");

    /// <summary>
    /// Открывает браузер, ожидает входа пользователя и получает сессионную cookie.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Полученное значение сессионной cookie.</returns>
    public async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(UserDataDir);
        using var playwright = await Playwright.CreateAsync();

        var options = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = Environment.GetEnvironmentVariable("YBD_HEADLESS") == "1",
        };
        var browser = Environment.GetEnvironmentVariable("YBD_BROWSER");
        if (!string.IsNullOrWhiteSpace(browser) && !browser.Equals("chromium", StringComparison.OrdinalIgnoreCase))
        {
            options.Channel = browser;
        }

        IBrowserContext context;
        try
        {
            context = await playwright.Chromium.LaunchPersistentContextAsync(UserDataDir, options);
        }
        catch (PlaywrightException exception) when (exception.Message.Contains("Executable doesn't exist"))
        {
            logger.LogWarning("Chromium is not installed for Playwright, installing it now ...");
            await InstallChromiumAsync(cancellationToken);
            context = await playwright.Chromium.LaunchPersistentContextAsync(UserDataDir, options);
        }

        try
        {
            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
            await page.GotoAsync(
                $"https://{BookDownloader.BooksDomain}/",
                new PageGotoOptions { Timeout = 60_000, WaitUntil = WaitUntilState.DOMContentLoaded });

            var timeoutSeconds = int.TryParse(
                Environment.GetEnvironmentVariable("YBD_LOGIN_TIMEOUT_SECONDS"),
                out var configured) ? configured : 300;
            var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

            logger.LogInformation(
                "Browser opened at https://{Domain}. Waiting for the {CookieName} cookie ...",
                BookDownloader.BooksDomain,
                SessionCookie.Name);

            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cookies = await context.CookiesAsync($"https://{BookDownloader.BooksDomain}");
                var session = cookies.FirstOrDefault(c =>
                    c.Name == SessionCookie.Name && !string.IsNullOrEmpty(c.Value));

                if (session is not null)
                {
                    store.Cookie = session.Value;
                    logger.LogInformation("{CookieName} cookie captured from the browser session.", SessionCookie.Name);
                    return session.Value;
                }

                await Task.Delay(1000, cancellationToken);
            }

            throw new LoginTimeoutException(
                $"Timed out waiting for login at https://{BookDownloader.BooksDomain}. " +
                "Please sign in inside the opened browser window and try again.");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private async Task InstallChromiumAsync(CancellationToken cancellationToken)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "playwright.ps1");
        if (!File.Exists(script))
        {
            throw new InvalidOperationException(
                $"Playwright browser installer '{script}' was not found. " +
                "Set YBD_BROWSER=msedge or YBD_BROWSER=chrome to use an installed browser instead.");
        }

        foreach (var shell in new[] { "pwsh", "powershell" })
        {
            try
            {
                var startInfo = new ProcessStartInfo(
                    shell,
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" install chromium")
                {
                    WorkingDirectory = AppContext.BaseDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                using var process = Process.Start(startInfo)!;
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data is not null)
                    {
                        logger.LogDebug("playwright: {Line}", e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data is not null)
                    {
                        logger.LogDebug("playwright: {Line}", e.Data);
                    }
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    logger.LogInformation("Chromium installed for Playwright.");
                    return;
                }
            }
            catch (Win32Exception)
            {
                continue;
            }
        }

        throw new InvalidOperationException(
            "Failed to install Chromium for Playwright. " +
            "Run 'playwright.ps1 install chromium' manually or set YBD_BROWSER=msedge or YBD_BROWSER=chrome.");
    }
}
