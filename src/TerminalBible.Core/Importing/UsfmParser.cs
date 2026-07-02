using System.Text.RegularExpressions;
using TerminalBible.Core.Domain;

namespace TerminalBible.Core.Importing;

public sealed partial class UsfmParser
{
    public IReadOnlyList<BibleBook> Parse(IEnumerable<UsfmDocument> documents)
    {
        var books = documents
            .Select((document, index) => ParseBook(document, index + 1))
            .Where(book => book.Chapters.Count > 0)
            .OrderBy(book => book.Order)
            .ToList();

        if (books.Count == 0)
        {
            throw new InvalidOperationException("Nenhum livro bíblico válido foi encontrado no pacote USFM.");
        }

        return books;
    }

    public BibleBook ParseBook(UsfmDocument document, int fallbackOrder = 999)
    {
        var code = Path.GetFileNameWithoutExtension(document.FileName).ToUpperInvariant();
        var name = string.Empty;
        var chapters = new List<BibleChapterBuilder>();
        BibleChapterBuilder? currentChapter = null;
        BibleVerseBuilder? currentVerse = null;

        foreach (var rawLine in SplitLines(document.Content))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (TryReadMarkerValue(line, "id", out var idValue))
            {
                var parts = idValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    code = parts[0].ToUpperInvariant();
                }

                continue;
            }

            if (TryReadMarkerValue(line, "toc2", out var toc2Value) || TryReadMarkerValue(line, "toc1", out toc2Value) || TryReadMarkerValue(line, "h", out toc2Value))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = CleanText(toc2Value);
                }

                continue;
            }

            if (TryReadMarkerValue(line, "c", out var chapterValue) && int.TryParse(chapterValue.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out var chapterNumber))
            {
                currentChapter = new BibleChapterBuilder(code, chapterNumber);
                chapters.Add(currentChapter);
                currentVerse = null;
                continue;
            }

            var verseMatch = VerseMarkerRegex().Match(line);
            if (verseMatch.Success)
            {
                if (currentChapter is null)
                {
                    currentChapter = new BibleChapterBuilder(code, 1);
                    chapters.Add(currentChapter);
                }

                currentVerse = new BibleVerseBuilder(int.Parse(verseMatch.Groups["number"].Value));
                currentVerse.Append(CleanText(verseMatch.Groups["text"].Value));
                currentChapter.Verses.Add(currentVerse);
                continue;
            }

            var continuation = CleanText(line);
            if (continuation.Length > 0 && currentVerse is not null)
            {
                currentVerse.Append(continuation);
            }
        }

        var resolvedName = string.IsNullOrWhiteSpace(name) ? BibleBookCatalog.GetName(code) : name;
        var order = BibleBookCatalog.GetOrder(code, fallbackOrder);

        return new BibleBook(
            code,
            resolvedName,
            order,
            chapters
                .Select(chapter => chapter.ToChapter())
                .Where(chapter => chapter.Verses.Count > 0)
                .OrderBy(chapter => chapter.Number)
                .ToList());
    }

    private static IEnumerable<string> SplitLines(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }

    private static bool TryReadMarkerValue(string line, string marker, out string value)
    {
        var prefix = "\\" + marker;
        if (line.StartsWith(prefix + " ", StringComparison.Ordinal) || string.Equals(line, prefix, StringComparison.Ordinal))
        {
            value = line.Length == prefix.Length ? string.Empty : line[(prefix.Length + 1)..].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string CleanText(string value)
    {
        var withoutMarkers = UsfmMarkerRegex().Replace(value, " ");
        var withoutAttributes = UsfmAttributeRegex().Replace(withoutMarkers, string.Empty);
        var normalized = WhitespaceRegex().Replace(withoutAttributes, " ").Trim();
        return normalized;
    }

    [GeneratedRegex(@"^\\v\s+(?<number>\d+)(?:[-,\w]*)?\s*(?<text>.*)$")]
    private static partial Regex VerseMarkerRegex();

    [GeneratedRegex(@"\\[a-z0-9]+\*?(?:\s+)?", RegexOptions.IgnoreCase)]
    private static partial Regex UsfmMarkerRegex();

    [GeneratedRegex(@"\|[^\s]+")]
    private static partial Regex UsfmAttributeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private sealed class BibleChapterBuilder(string bookCode, int number)
    {
        public List<BibleVerseBuilder> Verses { get; } = [];

        public BibleChapter ToChapter()
        {
            return new BibleChapter(
                bookCode,
                number,
                Verses.Select(verse => verse.ToVerse()).Where(verse => verse.Text.Length > 0).ToList());
        }
    }

    private sealed class BibleVerseBuilder(int number)
    {
        private readonly List<string> _parts = [];

        public void Append(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _parts.Add(text);
            }
        }

        public BibleVerse ToVerse()
        {
            return new BibleVerse(number, string.Join(' ', _parts));
        }
    }
}
