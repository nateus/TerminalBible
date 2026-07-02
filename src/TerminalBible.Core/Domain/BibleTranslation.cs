namespace TerminalBible.Core.Domain;

public sealed record BibleTranslation(
    string Id,
    string Name,
    string Language,
    string Source,
    string License,
    DateTimeOffset ImportedAt);
