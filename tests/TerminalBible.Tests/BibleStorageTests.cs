using TerminalBible.Core.Domain;
using TerminalBible.Core.Storage;

namespace TerminalBible.Tests;

public sealed class BibleStorageTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "TerminalBible.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoadAsync_PersistsTranslationAndBooks()
    {
        var storage = new BibleStorage(_rootPath);
        var translation = new BibleTranslation("test", "Teste", "pt-BR", "local", "teste", DateTimeOffset.UtcNow);
        var books = new[]
        {
            new BibleBook("GEN", "Gênesis", 1, [new BibleChapter("GEN", 1, [new BibleVerse(1, "No princípio.")])])
        };

        await storage.SaveAsync(translation, books);

        Assert.True(storage.IsInstalled("test"));
        var loadedTranslation = await storage.LoadTranslationAsync("test");
        var loadedBooks = await storage.LoadBooksAsync("test");
        var loadedBook = await storage.LoadBookAsync("test", "GEN");

        Assert.Equal("Teste", loadedTranslation?.Name);
        Assert.Single(loadedBooks);
        Assert.Equal("No princípio.", loadedBook?.Chapters[0].Verses[0].Text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
