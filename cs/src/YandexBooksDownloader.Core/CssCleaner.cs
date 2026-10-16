namespace YandexBooksDownloader.Core;

/// <summary>
/// Очищает css-файлы в каталоге скачанной книги.
/// </summary>
public static class CssCleaner
{
    /// <summary>
    /// Записывает пустое содержимое во все css-файлы, найденные рекурсивно.
    /// </summary>
    /// <param name="directory">Корневой каталог для поиска.</param>
    public static void ClearAll(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.css", SearchOption.AllDirectories))
        {
            File.WriteAllText(file, string.Empty);
        }
    }
}
