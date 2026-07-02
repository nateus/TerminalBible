using TerminalBible.Core.Domain;

namespace TerminalBible.Core.Reading;

public static class ReadingPaginator
{
    private const int MinimumWidth = 20;
    private const int MinimumHeight = 1;

    public static IReadOnlyList<ReadingPage> CreatePages(BibleChapter chapter, ReadingLayoutOptions options)
    {
        var width = Math.Max(MinimumWidth, options.Width);
        var height = Math.Max(MinimumHeight, options.Height);
        var tokens = BuildTokens(chapter);
        var lines = WrapTokens(tokens, width);
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
            if (!page.Tokens[index].IsVerseNumber)
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
        while (true)
        {
            var next = index + delta;
            if (next < 0 || next >= page.Tokens.Count)
            {
                return page.Tokens[index].IsVerseNumber ? GetFirstWordIndex(page) : index;
            }

            index = next;
            if (!page.Tokens[index].IsVerseNumber)
            {
                return index;
            }
        }
    }

    private static IReadOnlyList<ReadingToken> BuildTokens(BibleChapter chapter)
    {
        var tokens = new List<ReadingToken>();
        foreach (var verse in chapter.Verses)
        {
            tokens.Add(new ReadingToken(verse.Number, verse.Number.ToString(), IsVerseNumber: true));

            foreach (var word in verse.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                tokens.Add(new ReadingToken(verse.Number, word, IsVerseNumber: false));
            }
        }

        return tokens;
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
}
