using System.Text.RegularExpressions;
using TerminalBible.Core.Domain;

namespace TerminalBible.Core.Reading;

public static partial class ReadingPaginator
{
    private const int MinimumWidth = 20;
    private const int MinimumHeight = 1;
    private const int MaximumComfortableWidth = 96;

    public static IReadOnlyList<ReadingPage> CreatePages(BibleChapter chapter, ReadingLayoutOptions options)
    {
        var width = Math.Clamp(options.Width, MinimumWidth, MaximumComfortableWidth);
        var height = Math.Max(MinimumHeight, options.Height);
        var paragraphs = BuildParagraphs(chapter);
        var lines = WrapParagraphs(paragraphs, width);
        var pages = new List<ReadingPage>();

        for (var index = 0; index < lines.Count; index += height)
        {
            pages.Add(new ReadingPage(lines.Skip(index).Take(height).ToList()));
        }

        return pages.Count == 0 ? [new ReadingPage([])] : pages;
    }

    public static int GetFirstWordIndex(ReadingPage page)
    {
        for (var index = 0; index < page.Tokens.Count; index++)
        {
            if (IsSelectable(page.Tokens[index]))
            {
                return index;
            }
        }

        return -1;
    }

    public static int MoveCursor(ReadingPage page, int currentIndex, int delta)
    {
        if (page.Tokens.Count == 0)
        {
            return -1;
        }

        var index = Math.Clamp(currentIndex, 0, page.Tokens.Count - 1);
        var currentPhraseId = page.Tokens[index].PhraseId;
        var lastSelectable = IsSelectable(page.Tokens[index]) ? index : GetFirstWordIndex(page);
        while (true)
        {
            var next = index + delta;
            if (next < 0 || next >= page.Tokens.Count)
            {
                return lastSelectable;
            }

            index = next;
            if (IsSelectable(page.Tokens[index]) && page.Tokens[index].PhraseId != currentPhraseId)
            {
                lastSelectable = index;
                return index;
            }
        }
    }

    private static IReadOnlyList<IReadOnlyList<ReadingToken>> BuildParagraphs(BibleChapter chapter)
    {
        var paragraphs = new List<IReadOnlyList<ReadingToken>>();
        var currentParagraph = new List<ReadingToken>();
        var phraseId = 0;

        foreach (var verse in chapter.Verses)
        {
            if ((verse.StartsParagraph || ShouldInferParagraphStart(verse, currentParagraph)) && currentParagraph.Count > 0)
            {
                paragraphs.Add(currentParagraph);
                currentParagraph = [];
            }

            currentParagraph.Add(new ReadingToken(verse.Number, verse.Number.ToString(), IsVerseNumber: true, PhraseId: -1));

            foreach (var phrase in SplitIntoPhrases(verse.Text))
            {
                foreach (var word in phrase.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    currentParagraph.Add(new ReadingToken(verse.Number, word, IsVerseNumber: false, phraseId));
                }

                phraseId++;
            }
        }

        if (currentParagraph.Count > 0)
        {
            paragraphs.Add(currentParagraph);
        }

        return paragraphs;
    }

    private static IReadOnlyList<IReadOnlyList<ReadingToken>> WrapParagraphs(IReadOnlyList<IReadOnlyList<ReadingToken>> paragraphs, int width)
    {
        var lines = new List<IReadOnlyList<ReadingToken>>();
        foreach (var paragraph in paragraphs)
        {
            if (lines.Count > 0)
            {
                lines.Add([]);
            }

            lines.AddRange(WrapTokens(paragraph, width));
        }

        return lines;
    }

    private static IReadOnlyList<IReadOnlyList<ReadingToken>> WrapTokens(IReadOnlyList<ReadingToken> tokens, int width)
    {
        var lines = new List<IReadOnlyList<ReadingToken>>();
        var currentLine = new List<ReadingToken>();
        var currentWidth = 0;

        foreach (var token in tokens)
        {
            var tokenWidth = token.Text.Length;
            var projectedWidth = currentLine.Count == 0
                ? tokenWidth
                : currentWidth + 1 + tokenWidth;

            if (currentLine.Count > 0 && projectedWidth > width)
            {
                lines.Add(currentLine);
                currentLine = [];
                currentWidth = 0;
            }

            currentLine.Add(token);
            currentWidth = currentLine.Count == 1
                ? tokenWidth
                : currentWidth + 1 + tokenWidth;
        }

        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    private static IEnumerable<string> SplitIntoPhrases(string text)
    {
        foreach (Match match in PhraseRegex().Matches(text))
        {
            var normalized = match.Value.Trim();
            if (normalized.Length > 0)
            {
                yield return normalized;
            }
        }
    }

    private static bool IsSelectable(ReadingToken token)
    {
        return !token.IsVerseNumber;
    }

    private static bool ShouldInferParagraphStart(BibleVerse verse, IReadOnlyList<ReadingToken> currentParagraph)
    {
        if (currentParagraph.Count < 8)
        {
            return false;
        }

        var text = verse.Text.TrimStart();
        return text.StartsWith("E disse Deus", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Disse Deus", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Então disse", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Entao disse", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("E falou", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Então falou", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("Entao falou", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[^.!?;:]+[.!?;:]?", RegexOptions.CultureInvariant)]
    private static partial Regex PhraseRegex();
}
