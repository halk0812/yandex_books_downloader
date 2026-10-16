using System.Xml.Linq;

namespace YandexBooksDownloader.Core;

/// <summary>
/// Читает элементы манифеста из OPF-документа.
/// </summary>
public static class OpfItems
{
    /// <summary>
    /// Возвращает href всех элементов манифеста в порядке документа, пропуская элементы без href.
    /// </summary>
    /// <param name="opfXml">Содержимое OPF-документа.</param>
    /// <returns>Href элементов в порядке документа.</returns>
    public static IReadOnlyList<string> GetItemHrefs(string opfXml)
    {
        var document = XDocument.Parse(opfXml);
        var hrefs = new List<string>();

        foreach (var item in document.Descendants().Where(e => e.Name.LocalName == "item"))
        {
            var href = item.Attribute("href")?.Value;
            if (!string.IsNullOrEmpty(href))
            {
                hrefs.Add(href);
            }
        }

        return hrefs;
    }
}
