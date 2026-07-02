using TerminalBible.Core.Domain;

namespace TerminalBible.Core.Navigation;

public static class BibleNavigator
{
    public static ChapterReference? GetNextChapter(IReadOnlyList<BibleBook> books, ChapterReference current)
    {
        var bookIndex = FindBookIndex(books, current.BookCode);
        if (bookIndex < 0)
        {
            return null;
        }

        var book = books[bookIndex];
        var currentChapterIndex = FindChapterIndex(book, current.ChapterNumber);
        if (currentChapterIndex < 0)
        {
            return null;
        }

        if (currentChapterIndex + 1 < book.Chapters.Count)
        {
            return new ChapterReference(book.Code, book.Chapters[currentChapterIndex + 1].Number);
        }

        if (bookIndex + 1 < books.Count && books[bookIndex + 1].Chapters.Count > 0)
        {
            var nextBook = books[bookIndex + 1];
            return new ChapterReference(nextBook.Code, nextBook.Chapters[0].Number);
        }

        return null;
    }

    public static ChapterReference? GetPreviousChapter(IReadOnlyList<BibleBook> books, ChapterReference current)
    {
        var bookIndex = FindBookIndex(books, current.BookCode);
        if (bookIndex < 0)
        {
            return null;
        }

        var book = books[bookIndex];
        var currentChapterIndex = FindChapterIndex(book, current.ChapterNumber);
        if (currentChapterIndex < 0)
        {
            return null;
        }

        if (currentChapterIndex > 0)
        {
            return new ChapterReference(book.Code, book.Chapters[currentChapterIndex - 1].Number);
        }

        if (bookIndex > 0 && books[bookIndex - 1].Chapters.Count > 0)
        {
            var previousBook = books[bookIndex - 1];
            return new ChapterReference(previousBook.Code, previousBook.Chapters[^1].Number);
        }

        return null;
    }

    private static int FindBookIndex(IReadOnlyList<BibleBook> books, string bookCode)
    {
        for (var index = 0; index < books.Count; index++)
        {
            if (string.Equals(books[index].Code, bookCode, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindChapterIndex(BibleBook book, int chapterNumber)
    {
        for (var index = 0; index < book.Chapters.Count; index++)
        {
            if (book.Chapters[index].Number == chapterNumber)
            {
                return index;
            }
        }

        return -1;
    }
}
