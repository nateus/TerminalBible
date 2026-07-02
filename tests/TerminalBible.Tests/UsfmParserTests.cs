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
}
