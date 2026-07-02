namespace TerminalBible.Core.Domain;

public sealed record ChapterReference(
    string BookCode,
    int ChapterNumber);
