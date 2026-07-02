namespace TerminalBible.Core.Domain;

public sealed record BibleChapter(
    string BookCode,
    int Number,
    IReadOnlyList<BibleVerse> Verses);
