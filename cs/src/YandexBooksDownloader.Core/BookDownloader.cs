using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace YandexBooksDownloader.Core;

/// <summary>
/// Скачивает книгу с books.yandex.ru и сохраняет её как распакованную структуру epub.
/// </summary>
public sealed class BookDownloader
{
    /// <summary>
    /// Домен Яндекс Книг.
    /// </summary>
    public const string BooksDomain = "books.yandex.ru";

    private readonly Downloader _downloader;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookDownloader"/> class.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <param name="downloader">Загрузчик, сохраняющий файлы книги.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="secret">Необязательный секрет расшифровки, скачивается если не задан.</param>
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

    /// <summary>
    /// Идентификатор книги.
    /// </summary>
    public string BookId { get; }

    /// <summary>
    /// Секрет расшифровки, устанавливается после скачивания секрета.
    /// </summary>
    public string? Secret { get; private set; }

    /// <summary>
    /// Путь epub-файла книги, создаваемого при упаковке.
    /// </summary>
    public string EpubPath => _downloader.OutDir + ".epub";

    /// <summary>
    /// Скачивает секрет расшифровки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Скачанный секрет.</returns>
    public async Task<string> DownloadSecretAsync(CancellationToken cancellationToken)
    {
        var url = $"https://{BooksDomain}/reader/p/api/v5/metadata_secret?lang=ru";
        var json = await _downloader.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("secret", out var secretElement))
        {
            throw new InvalidDataException($"Secret is missing in response from '{url}'.");
        }

        Secret = secretElement.GetString() ?? throw new InvalidDataException("Secret is null.");
        _logger.LogDebug("secret: {Secret}", Secret);
        return Secret;
    }

    /// <summary>
    /// Скачивает метаданные книги и все файлы книги.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task DownloadAsync(CancellationToken cancellationToken)
    {
        var secret = Secret ?? await DownloadSecretAsync(cancellationToken).ConfigureAwait(false);
        var metadataUrl = $"https://{BooksDomain}/p/api/v5/books/{BookId}/metadata/v4";
        var metadataJson = await _downloader.GetStringAsync(metadataUrl, cancellationToken).ConfigureAwait(false);
        var metadata = BookMetadata.FromJson(metadataJson, secret);
        await ProcessMetadataAsync(metadata, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Удаляет каталог скачанной книги.
    /// </summary>
    public void DeleteDownloaded() => _downloader.DeleteDownloaded();

    /// <summary>
    /// Очищает css-файлы скачанной книги.
    /// </summary>
    public void ClearCss() => _downloader.ClearCss();

    /// <summary>
    /// Упаковывает скачанную книгу в epub-файл.
    /// </summary>
    public void MakeEpub() => _downloader.MakeEpub();

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
}
