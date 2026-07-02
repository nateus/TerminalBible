using TerminalBible.Core.Domain;
using TerminalBible.Core.Reading;

namespace TerminalBible.Tests;

public sealed class ReadingPaginatorTests
{
    [Fact]
    public void CreatePages_RespectsRequestedWidthAndHeight()
    {
        var chapter = CreateChapter();
        var pages = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(24, 2));

        Assert.All(pages, page =>
        {
            Assert.True(page.Lines.Count <= 2);
            Assert.All(page.Lines, line =>
            {
                var width = string.Join(' ', line.Select(token => token.Text)).Length;
                Assert.True(width <= 24);
            });
        });
    }

    [Fact]
    public void CreatePages_FlowsVerseNumbersInline()
    {
        var chapter = CreateChapter();
        var pages = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(80, 10));

        var firstLine = string.Join(' ', pages[0].Lines[0].Select(token => token.Text));

        Assert.StartsWith("1 No principio Deus criou", firstLine);
        Assert.Contains("2 A terra", firstLine);
    }

    [Fact]
    public void CreatePages_DoesNotLoseOrDuplicateTokens()
    {
        var chapter = CreateChapter();
        var pages = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(18, 2));

        var actual = pages
            .SelectMany(page => page.Tokens)
            .Select(token => token.Text)
            .ToArray();

        Assert.Equal(
            ["1", "No", "principio", "Deus", "criou", "os", "ceus.", "2", "A", "terra", "era", "sem", "forma", "e", "vazia."],
            actual);
    }

    [Fact]
    public void MoveCursor_StaysWithinVisiblePageWords()
    {
        var chapter = CreateChapter();
        var page = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(18, 2))[0];
        var firstWord = ReadingPaginator.GetFirstWordIndex(page);
        var lastWord = page.Tokens.Count - 1;

        var movedLeft = ReadingPaginator.MoveCursor(page, firstWord, -1);
        var movedRight = firstWord;
        for (var index = 0; index < 20; index++)
        {
            movedRight = ReadingPaginator.MoveCursor(page, movedRight, 1);
        }

        Assert.Equal(firstWord, movedLeft);
        Assert.Equal(lastWord, movedRight);
        Assert.False(page.Tokens[movedRight].IsVerseNumber);
    }

    private static BibleChapter CreateChapter()
    {
        return new BibleChapter("GEN", 1,
        [
            new BibleVerse(1, "No principio Deus criou os ceus."),
            new BibleVerse(2, "A terra era sem forma e vazia.")
        ]);
    }
}
