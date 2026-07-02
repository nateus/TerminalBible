namespace TerminalBible.Core.Importing;

public sealed record BibleSource(
    string Id,
    string Name,
    string Language,
    string Source,
    string License,
    Uri DownloadUri)
{
    public static BibleSource PortugueseBible { get; } = new(
        "porbr2018",
        "Bíblia Livre",
        "pt-BR",
        "eBible.org",
        "Creative Commons Atribuição 4.0",
        new Uri("https://ebible.org/Scriptures/porbr2018_usfm.zip"));
}
