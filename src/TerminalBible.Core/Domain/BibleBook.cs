namespace TerminalBible.Core.Domain;

public sealed record BibleBook(
    string Code,
    string Name,
    int Order,
    IReadOnlyList<BibleChapter> Chapters);
