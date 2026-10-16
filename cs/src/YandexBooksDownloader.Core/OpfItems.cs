using System.Xml.Linq;

namespace YandexBooksDownloader.Core;

public static class OpfItems
{
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
