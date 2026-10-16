namespace YandexBooksDownloader.Core;

public static class CssCleaner
{
    public static void ClearAll(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.css", SearchOption.AllDirectories))
        {
            File.WriteAllText(file, string.Empty);
        }
    }
}
