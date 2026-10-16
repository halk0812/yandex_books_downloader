using Microsoft.Extensions.Logging;

namespace YandexBooksDownloader.Core;

public sealed class Downloader
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;

    public Downloader(HttpClient http, string outDir, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(outDir);

        _http = http;
        OutDir = Path.GetFullPath(outDir);
        _logger = logger;
    }

    public string OutDir { get; }

    public string PathOf(string sub) => Path.Combine(OutDir, sub);

    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        return System.Text.Encoding.UTF8.GetString(await GetBytesAsync(url, cancellationToken).ConfigureAwait(false));
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("downloading {Url} ...", url);
        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("response: {Status} {Url}", (int)response.StatusCode, url);

        if ((int)response.StatusCode != 200)
        {
            throw new HttpRequestException($"Request to '{url}' failed with status {(int)response.StatusCode}.", null, response.StatusCode);
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

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

    public void DeleteDownloaded()
    {
        if (Directory.Exists(OutDir))
        {
            Directory.Delete(OutDir, recursive: true);
        }
    }

    public void ClearCss() => CssCleaner.ClearAll(OutDir);

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
