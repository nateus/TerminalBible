namespace TerminalBible.Core.Reading;

public sealed record ReadingPage(
    IReadOnlyList<IReadOnlyList<ReadingToken>> Lines)
{
    public IReadOnlyList<ReadingToken> Tokens { get; } = Lines.SelectMany(line => line).ToList();
}
