using System.IO.Compression;
using TerminalBible.Core.Domain;
using TerminalBible.Core.Storage;

namespace TerminalBible.Core.Importing;

public sealed class BibleInstallService(
    BibleStorage storage,
    UsfmParser parser,
    IBiblePackageDownloader downloader)
{
    public async Task<BibleTranslation> InstallAsync(BibleSource source, CancellationToken cancellationToken = default)
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "TerminalBible", Guid.NewGuid().ToString("N"));
        var zipPath = Path.Combine(temporaryRoot, $"{source.Id}.zip");
        var extractPath = Path.Combine(temporaryRoot, "extract");

        Directory.CreateDirectory(temporaryRoot);
        Directory.CreateDirectory(extractPath);

        try
        {
            await downloader.DownloadAsync(source, zipPath, cancellationToken);
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            var documents = Directory
                .EnumerateFiles(extractPath, "*.*", SearchOption.AllDirectories)
                .Where(IsUsfmFile)
                .Select(file => new UsfmDocument(Path.GetFileName(file), File.ReadAllText(file)))
                .ToList();

            if (documents.Count == 0)
            {
                throw new InvalidOperationException("O pacote baixado não contém arquivos USFM.");
            }

            var books = parser.Parse(documents);
            var translation = new BibleTranslation(
                source.Id,
                source.Name,
                source.Language,
                source.Source,
                source.License,
                DateTimeOffset.UtcNow);

            await storage.SaveAsync(translation, books, cancellationToken);
            return translation;
        }
        finally
        {
            TryDeleteDirectory(temporaryRoot);
        }
    }

    private static bool IsUsfmFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".usfm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".sfm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".SFM", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
