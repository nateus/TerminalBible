namespace TerminalBible.Core.Importing;

public sealed class HttpBiblePackageDownloader(HttpClient httpClient) : IBiblePackageDownloader
{
    public async Task DownloadAsync(BibleSource source, string destinationPath, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(source.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(fileStream, cancellationToken);
    }
}
