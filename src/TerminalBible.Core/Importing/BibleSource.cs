namespace TerminalBible.Core.Importing;

public sealed record BibleSource(
    string Id,
    string Name,
    string Language,
    string Source,
    string License,
    Uri DownloadUri)
{
    public static BibleSource PortugueseWorldBible { get; } = new(
        "porbrbsl",
        "Bíblia Portuguesa Mundial",
        "pt-BR",
        "eBible.org",
        "Domínio público",
        new Uri("https://ebible.org/Scriptures/porbrbsl_usfm.zip"));
}
