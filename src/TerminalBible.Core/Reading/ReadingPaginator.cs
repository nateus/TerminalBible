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
        var paragraphs = BuildParagraphs(chapter, width);
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
        var lastSelectable = IsSelectable(page.Tokens[index]) ? index : GetFirstWordIndex(page);
        while (true)
        {
            var next = index + delta;
            if (next < 0 || next >= page.Tokens.Count)
            {
                return lastSelectable;
            }

            index = next;
            if (IsSelectable(page.Tokens[index]))
            {
                lastSelectable = index;
                return index;
            }
        }
    }

    private static IReadOnlyList<IReadOnlyList<ReadingToken>> BuildParagraphs(BibleChapter chapter, int width)
    {
        var paragraphs = new List<IReadOnlyList<ReadingToken>>();
        var currentParagraph = new List<ReadingToken>();

        foreach (var verse in chapter.Verses)
        {
            if (verse.StartsParagraph && currentParagraph.Count > 0)
            {
                paragraphs.Add(currentParagraph);
                currentParagraph = [];
            }

            currentParagraph.Add(new ReadingToken(verse.Number, verse.Number.ToString(), IsVerseNumber: true));

            foreach (var phrase in SplitIntoPhrases(verse.Text))
            {
                foreach (var chunk in SplitLongPhrase(phrase, width))
                {
                    currentParagraph.Add(new ReadingToken(verse.Number, chunk, IsVerseNumber: false));
                }
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

    private static IEnumerable<string> SplitLongPhrase(string phrase, int width)
    {
        var words = phrase.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var current = new List<string>();
        var currentWidth = 0;

        foreach (var word in words)
        {
            var projectedWidth = current.Count == 0 ? word.Length : currentWidth + 1 + word.Length;
            if (current.Count > 0 && projectedWidth > width)
            {
                yield return string.Join(' ', current);
                current = [];
                currentWidth = 0;
            }

            current.Add(word);
            currentWidth = current.Count == 1 ? word.Length : currentWidth + 1 + word.Length;
        }

        if (current.Count > 0)
        {
            yield return string.Join(' ', current);
        }
    }

    private static bool IsSelectable(ReadingToken token)
    {
        return !token.IsVerseNumber;
    }

    [GeneratedRegex(@"[^.!?;:]+[.!?;:]?", RegexOptions.CultureInvariant)]
    private static partial Regex PhraseRegex();
}
