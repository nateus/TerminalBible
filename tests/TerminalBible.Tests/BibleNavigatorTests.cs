using TerminalBible.Core.Domain;
using TerminalBible.Core.Navigation;

namespace TerminalBible.Tests;

public sealed class BibleNavigatorTests
{
    private static readonly IReadOnlyList<BibleBook> Books =
    [
        new BibleBook("GEN", "Gênesis", 1,
        [
            new BibleChapter("GEN", 1, [new BibleVerse(1, "A")]),
            new BibleChapter("GEN", 2, [new BibleVerse(1, "B")])
        ]),
        new BibleBook("EXO", "Êxodo", 2,
        [
            new BibleChapter("EXO", 1, [new BibleVerse(1, "C")])
        ])
    ];

    [Fact]
    public void GetNextChapter_ReturnsNextChapterInSameBook()
    {
        var next = BibleNavigator.GetNextChapter(Books, new ChapterReference("GEN", 1));

        Assert.Equal(new ChapterReference("GEN", 2), next);
    }

    [Fact]
    public void GetNextChapter_MovesToFirstChapterOfNextBook()
    {
        var next = BibleNavigator.GetNextChapter(Books, new ChapterReference("GEN", 2));

        Assert.Equal(new ChapterReference("EXO", 1), next);
    }

    [Fact]
    public void GetPreviousChapter_ReturnsPreviousChapterInSameBook()
    {
        var previous = BibleNavigator.GetPreviousChapter(Books, new ChapterReference("GEN", 2));

        Assert.Equal(new ChapterReference("GEN", 1), previous);
    }

    [Fact]
    public void GetPreviousChapter_MovesToLastChapterOfPreviousBook()
    {
        var previous = BibleNavigator.GetPreviousChapter(Books, new ChapterReference("EXO", 1));

        Assert.Equal(new ChapterReference("GEN", 2), previous);
    }

    [Fact]
    public void Navigation_ReturnsNullAtBibleBoundaries()
    {
        Assert.Null(BibleNavigator.GetPreviousChapter(Books, new ChapterReference("GEN", 1)));
        Assert.Null(BibleNavigator.GetNextChapter(Books, new ChapterReference("EXO", 1)));
    }
}
