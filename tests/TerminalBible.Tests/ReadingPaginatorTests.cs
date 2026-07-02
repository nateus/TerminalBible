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

        Assert.StartsWith("1 No principio Deus criou os ceus.", firstLine);
        Assert.Contains("2 A terra era sem forma e vazia.", firstLine);
    }

    [Fact]
    public void CreatePages_DoesNotLoseOrDuplicateWords()
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
    public void CreatePages_AddsSpaceBetweenParagraphs()
    {
        var chapter = new BibleChapter("GEN", 1,
        [
            new BibleVerse(1, "Primeiro paragrafo."),
            new BibleVerse(2, "Segundo paragrafo.") { StartsParagraph = true }
        ]);

        var page = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(80, 10))[0];

        Assert.Contains(page.Lines, line => line.Count == 0);
    }

    [Fact]
    public void CreatePages_InferParagraphsForRepeatedNarrativeSpeech()
    {
        var chapter = new BibleChapter("GEN", 1,
        [
            new BibleVerse(1, "No principio criou Deus os ceus e a terra."),
            new BibleVerse(2, "E a terra estava sem forma e vazia."),
            new BibleVerse(3, "E disse Deus: Haja luz; e houve luz."),
            new BibleVerse(4, "E viu Deus que a luz era boa.")
        ]);

        var page = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(80, 10))[0];

        Assert.Contains(page.Lines, line => line.Count == 0);
    }

    [Fact]
    public void MoveCursor_StaysWithinVisiblePagePhrases()
    {
        var chapter = CreateChapter();
        var page = ReadingPaginator.CreatePages(chapter, new ReadingLayoutOptions(80, 10))[0];
        var firstPhrase = ReadingPaginator.GetFirstWordIndex(page);
        var lastPhrase = page.Tokens
            .Select((token, index) => new { token, index })
            .First(item => item.token.PhraseId == 1)
            .index;

        var movedLeft = ReadingPaginator.MoveCursor(page, firstPhrase, -1);
        var movedRight = firstPhrase;
        for (var index = 0; index < 20; index++)
        {
            movedRight = ReadingPaginator.MoveCursor(page, movedRight, 1);
        }

        Assert.Equal(firstPhrase, movedLeft);
        Assert.Equal(lastPhrase, movedRight);
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
