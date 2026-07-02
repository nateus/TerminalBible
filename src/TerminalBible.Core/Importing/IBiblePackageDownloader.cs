namespace TerminalBible.Core.Importing;

public interface IBiblePackageDownloader
{
    Task DownloadAsync(BibleSource source, string destinationPath, CancellationToken cancellationToken = default);
}
