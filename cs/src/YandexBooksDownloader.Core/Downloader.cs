using System.Text;
using Microsoft.Extensions.Logging;

namespace YandexBooksDownloader.Core;

/// <summary>
/// Скачивает файлы по HTTP и сохраняет их в каталог вывода.
/// </summary>
public sealed class Downloader
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Downloader"/> class.
    /// </summary>
    /// <param name="http">HTTP-клиент.</param>
    /// <param name="outDir">Каталог вывода.</param>
    /// <param name="logger">Логгер.</param>
    public Downloader(HttpClient http, string outDir, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(outDir);

        _http = http;
        OutDir = Path.GetFullPath(outDir);
        _logger = logger;
    }

    /// <summary>
    /// Полный путь каталога вывода.
    /// </summary>
    public string OutDir { get; }

    /// <summary>
    /// Возвращает полный путь файла внутри каталога вывода.
    /// </summary>
    /// <param name="sub">Относительный путь внутри каталога вывода.</param>
    /// <returns>Полный путь.</returns>
    public string PathOf(string sub) => Path.Combine(OutDir, sub);

    /// <summary>
    /// Скачивает тело ответа как текст.
    /// </summary>
    /// <param name="url">URL запроса.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Тело ответа, раскодированное как UTF-8.</returns>
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        return Encoding.UTF8.GetString(await GetBytesAsync(url, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Скачивает тело ответа как байты.
    /// </summary>
    /// <param name="url">URL запроса.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Байты тела ответа.</returns>
    public async Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken)
    {
        _logger.LogDebug("downloading {Url} ...", url);
        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("response: {Status} {Url}", (int)response.StatusCode, url);

        if ((int)response.StatusCode != 200)
        {
            throw new HttpRequestException(
                $"Request to '{url}' failed with status {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Сохраняет байты в каталог вывода, создавая родительские каталоги.
    /// </summary>
    /// <param name="bytes">Байты для сохранения.</param>
    /// <param name="name">Относительный путь внутри каталога вывода.</param>
    public void SaveBytes(byte[] bytes, string name)
    {
        var filePath = PathOf(name);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(filePath, bytes);
    }

    /// <summary>
    /// Удаляет каталог вывода.
    /// </summary>
    public void DeleteDownloaded()
    {
        if (Directory.Exists(OutDir))
        {
            Directory.Delete(OutDir, recursive: true);
        }
    }

    /// <summary>
    /// Очищает css-файлы в каталоге вывода.
    /// </summary>
    public void ClearCss() => CssCleaner.ClearAll(OutDir);

    /// <summary>
    /// Упаковывает каталог вывода в epub-файл.
    /// </summary>
    public void MakeEpub()
    {
        if (!Directory.Exists(OutDir))
        {
            throw new DirectoryNotFoundException($"Output directory '{OutDir}' does not exist.");
        }

        var epubPath = OutDir + ".epub";
        EpubBuilder.Create(OutDir, epubPath);
        _logger.LogInformation("ebook saved as {EpubPath}", epubPath);
        _logger.LogInformation("We recommend https://calibre-ebook.com/ for book management and conversion");
    }
}
