namespace TerminalBible.Core.Reading;

public sealed record ReadingToken(
    int VerseNumber,
    string Text,
    bool IsVerseNumber,
    int PhraseId);
