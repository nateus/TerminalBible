using System.Text;
using Spectre.Console;
using TerminalBible;
using TerminalBible.Core.Importing;
using TerminalBible.Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

var storage = new BibleStorage(AppDataPaths.GetDefaultDataRoot());
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(60)
};

var installer = new BibleInstallService(
    storage,
    new UsfmParser(),
    new HttpBiblePackageDownloader(httpClient));

var app = new TerminalBibleApplication(storage, installer);
await app.RunAsync();
