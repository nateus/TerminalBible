using TerminalBible.Core.Importing;

namespace TerminalBible.Tests;

public sealed class UsfmParserTests
{
    [Fact]
    public void ParseBook_ReadsBookChaptersVersesAndContinuationLines()
    {
        var content = """
            \id GEN
            \toc2 Gênesis
            \c 1
            \p
            \v 1 No princípio Deus criou os céus e a terra.
            \v 2 A terra era sem forma e vazia.
            E havia trevas sobre a face do abismo.
            \c 2
            \v 1 Assim foram acabados os céus e a terra.
            """;

        var book = new UsfmParser().ParseBook(new UsfmDocument("GEN.usfm", content));

        Assert.Equal("GEN", book.Code);
        Assert.Equal("Gênesis", book.Name);
        Assert.Equal(1, book.Order);
        Assert.Equal(2, book.Chapters.Count);
        Assert.Equal(2, book.Chapters[0].Verses.Count);
        Assert.Equal("A terra era sem forma e vazia. E havia trevas sobre a face do abismo.", book.Chapters[0].Verses[1].Text);
    }

    [Fact]
    public void Parse_OrdersBooksByCanonicalOrder()
    {
        var documents = new[]
        {
            new UsfmDocument("MAT.usfm", "\\id MAT\n\\c 1\n\\v 1 Livro da genealogia de Jesus Cristo."),
            new UsfmDocument("GEN.usfm", "\\id GEN\n\\c 1\n\\v 1 No princípio.")
        };

        var books = new UsfmParser().Parse(documents);

        Assert.Equal(["GEN", "MAT"], books.Select(book => book.Code).ToArray());
    }

    [Fact]
    public void ParseBook_PreservesUsfmParagraphStarts()
    {
        var content = """
            \id MRK
            \c 1
            \p
            \v 1 Principio do evangelho de Jesus Cristo.
            \v 2 Conforme esta escrito no profeta Isaias.
            \m
            \v 3 Voz do que clama no deserto.
            """;

        var book = new UsfmParser().ParseBook(new UsfmDocument("MRK.usfm", content));
        var verses = book.Chapters[0].Verses;

        Assert.True(verses[0].StartsParagraph);
        Assert.False(verses[1].StartsParagraph);
        Assert.True(verses[2].StartsParagraph);
    }

    [Fact]
    public void ParseBook_RemovesFootnotesAndCrossReferencesFromVerseText()
    {
        var content = """
            \id MAT
            \c 1
            \p
            \v 21 Ela dará à luz um filho\f + \fr 1:21 \ft Nota explicativa.\f* e tu chamarás seu nome Jesus. \rq Isaías 7:14 \rq*
            """;

        var book = new UsfmParser().ParseBook(new UsfmDocument("MAT.usfm", content));

        Assert.Equal("Ela dará à luz um filho e tu chamarás seu nome Jesus.", book.Chapters[0].Verses[0].Text);
    }
}
