using System.IO.Compression;
using TerminalBible.Core.Importing;
using TerminalBible.Core.Storage;

namespace TerminalBible.Tests;

public sealed class BibleInstallServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "TerminalBible.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InstallAsync_UsesDownloaderParsesUsfmAndStoresOfflineBible()
    {
        var packagePath = CreatePackage();
        var storage = new BibleStorage(_rootPath);
        var service = new BibleInstallService(storage, new UsfmParser(), new FileCopyDownloader(packagePath));
        var source = new BibleSource("test", "Bíblia Teste", "pt-BR", "teste", "teste", new Uri("https://example.test/test.zip"));

        var translation = await service.InstallAsync(source);
        var books = await storage.LoadBooksAsync("test");

        Assert.Equal("test", translation.Id);
        Assert.True(storage.IsInstalled("test"));
        Assert.Single(books);
        Assert.Equal("GEN", books[0].Code);
        Assert.Equal("No princípio.", books[0].Chapters[0].Verses[0].Text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private string CreatePackage()
    {
        Directory.CreateDirectory(_rootPath);
        var packagePath = Path.Combine(_rootPath, "test.zip");

        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("GEN.usfm");
        using var writer = new StreamWriter(entry.Open());
        writer.Write("""
            \id GEN
            \toc2 Gênesis
            \c 1
            \v 1 No princípio.
            """);

        return packagePath;
    }

    private sealed class FileCopyDownloader(string sourcePath) : IBiblePackageDownloader
    {
        public Task DownloadAsync(BibleSource source, string destinationPath, CancellationToken cancellationToken = default)
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
            return Task.CompletedTask;
        }
    }
}
