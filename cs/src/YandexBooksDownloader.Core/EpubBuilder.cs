using System.IO.Compression;

namespace YandexBooksDownloader.Core;

/// <summary>
/// Упаковывает каталог скачанной книги в epub-файл.
/// </summary>
public static class EpubBuilder
{
    /// <summary>
    /// Mimetype контейнера epub.
    /// </summary>
    public const string MimeType = "application/epub+zip";

    /// <summary>
    /// Создаёт epub-файл из исходного каталога, размещая запись mimetype первой и без сжатия.
    /// </summary>
    /// <param name="sourceDir">Каталог с распакованным содержимым книги.</param>
    /// <param name="epubPath">Путь создаваемого epub-файла.</param>
    public static void Create(string sourceDir, string epubPath)
    {
        var top = Path.GetFullPath(sourceDir);
        if (!Directory.Exists(top))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDir}' does not exist.");
        }

        var files = Directory.EnumerateFiles(top, "*", SearchOption.AllDirectories).ToList();

        using var fileStream = File.Create(epubPath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        foreach (var file in files.Where(IsMimetype))
        {
            AddEntry(archive, top, file, CompressionLevel.NoCompression);
        }

        foreach (var file in files.Where(f => !IsMimetype(f)))
        {
            AddEntry(archive, top, file, CompressionLevel.SmallestSize);
        }
    }

    private static bool IsMimetype(string file) =>
        Path.GetFileName(file).Equals("mimetype", StringComparison.Ordinal);

    private static void AddEntry(ZipArchive archive, string top, string file, CompressionLevel level)
    {
        var entryName = Path.GetRelativePath(top, file).Replace('\\', '/');
        var entry = archive.CreateEntry(entryName, level);
        using var entryStream = entry.Open();
        using var source = File.OpenRead(file);
        source.CopyTo(entryStream);
    }
}
