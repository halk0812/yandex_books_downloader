using System.Diagnostics;

namespace YandexBooksDownloader.Core;

/// <summary>
/// Конвертирует epub в другие форматы утилитой Calibre ebook-convert.
/// </summary>
public static class EbookConverter
{
    /// <summary>
    /// Имя переменной окружения с путём к исполняемому файлу ebook-convert.
    /// </summary>
    public const string ConverterPathVariable = "YBD_CALIBRE";

    /// <summary>
    /// Ищет исполняемый файл Calibre ebook-convert.
    /// </summary>
    /// <returns>Путь к ebook-convert или null, если Calibre не найден.</returns>
    public static string? FindConverterPath()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ConverterPathVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && File.Exists(fromEnvironment))
        {
            return fromEnvironment;
        }

        var executableName = OperatingSystem.IsWindows() ? "ebook-convert.exe" : "ebook-convert";

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var standardPath = Path.Combine(programFiles, "Calibre2", executableName);
        if (File.Exists(standardPath))
        {
            return standardPath;
        }

        if (OperatingSystem.IsMacOS())
        {
            var bundlePath = Path.Combine("/Applications", "calibre.app", "Contents", "MacOS", executableName);
            if (File.Exists(bundlePath))
            {
                return bundlePath;
            }
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathVariable.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Проверяет доступность Calibre ebook-convert.
    /// </summary>
    /// <returns>Истина, если конвертер найден.</returns>
    public static bool IsConverterAvailable() => FindConverterPath() is not null;

    /// <summary>
    /// Конвертирует epub-файл в формат, определяемый по расширению целевого файла.
    /// </summary>
    /// <param name="epubPath">Путь исходного epub-файла.</param>
    /// <param name="targetPath">Путь целевого файла, например .mobi или .pdf.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public static async Task ConvertAsync(string epubPath, string targetPath, CancellationToken cancellationToken)
    {
        var converter = FindConverterPath()
            ?? throw new InvalidOperationException(
                $"Calibre ebook-convert was not found. Install https://calibre-ebook.com/ or set {ConverterPathVariable} to its path.");

        var startInfo = new ProcessStartInfo(converter, BuildArguments(epubPath, targetPath))
        {
            WorkingDirectory = Path.GetDirectoryName(converter),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)!;
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidDataException(
                $"ebook-convert failed with exit code {process.ExitCode}: {message.Trim()}");
        }
    }

    internal static string BuildArguments(string epubPath, string targetPath) =>
        $"\"{epubPath}\" \"{targetPath}\"";
}
