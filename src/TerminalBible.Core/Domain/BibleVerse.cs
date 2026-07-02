namespace TerminalBible.Core.Domain;

public sealed record BibleVerse(
    int Number,
    string Text)
{
    public bool StartsParagraph { get; init; }
}
