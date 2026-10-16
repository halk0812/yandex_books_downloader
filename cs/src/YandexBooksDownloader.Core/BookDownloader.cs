using System.Text;
using Microsoft.Extensions.Logging;

namespace YandexBooksDownloader.Core;

public sealed class BookDownloader
{
    public const string BooksDomain = "books.yandex.ru";

    private readonly Downloader _downloader;
    private readonly ILogger _logger;

    public BookDownloader(string bookId, Downloader downloader, ILogger logger, string? secret = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookId);
        ArgumentNullException.ThrowIfNull(downloader);
        ArgumentNullException.ThrowIfNull(logger);

        BookId = bookId;
        _downloader = downloader;
        _logger = logger;
        Secret = secret;
    }

    public string BookId { get; }

    public string? Secret { get; private set; }

    public async Task<string> DownloadSecretAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://{BooksDomain}/reader/p/api/v5/metadata_secret?lang=ru";
        var json = await _downloader.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("secret", out var secretElement))
        {
            throw new InvalidDataException($"Secret is missing in response from '{url}'.");
        }

        Secret = secretElement.GetString() ?? throw new InvalidDataException("Secret is null.");
        _logger.LogDebug("secret: {Secret}", Secret);
        return Secret;
    }

    public async Task DownloadAsync(CancellationToken cancellationToken = default)
    {
        var secret = Secret ?? await DownloadSecretAsync(cancellationToken).ConfigureAwait(false);
        var metadataUrl = $"https://{BooksDomain}/p/api/v5/books/{BookId}/metadata/v4";
        var metadataJson = await _downloader.GetStringAsync(metadataUrl, cancellationToken).ConfigureAwait(false);
        var metadata = BookMetadata.FromJson(metadataJson, secret);
        await ProcessMetadataAsync(metadata, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessMetadataAsync(BookMetadata metadata, CancellationToken cancellationToken)
    {
        _downloader.SaveBytes(Encoding.UTF8.GetBytes(EpubBuilder.MimeType), "mimetype");
        _downloader.SaveBytes(metadata.GetBytes("container"), "META-INF/container.xml");
        var opf = metadata.GetBytes("opf");
        _downloader.SaveBytes(opf, "OEBPS/content.opf");
        await ProcessOpfAsync(Encoding.UTF8.GetString(opf), metadata.GetString("document_uuid"), cancellationToken)
            .ConfigureAwait(false);
        _downloader.SaveBytes(metadata.GetBytes("ncx"), "OEBPS/toc.ncx");
    }

    private async Task ProcessOpfAsync(string opfXml, string uuid, CancellationToken cancellationToken)
    {
        foreach (var href in OpfItems.GetItemHrefs(opfXml))
        {
            if (href == "toc.ncx")
            {
                continue;
            }

            var url = $"https://{BooksDomain}/p/a/4/d/{uuid}/contents/OEBPS/{href}";
            try
            {
                var content = await _downloader.GetBytesAsync(url, cancellationToken).ConfigureAwait(false);
                _downloader.SaveBytes(content, Path.Combine("OEBPS", href.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning("cannot download from '{Url}': {Message}", url, exception.Message);
            }
        }
    }

    public void DeleteDownloaded() => _downloader.DeleteDownloaded();

    public void ClearCss() => _downloader.ClearCss();

    public void MakeEpub() => _downloader.MakeEpub();
}
