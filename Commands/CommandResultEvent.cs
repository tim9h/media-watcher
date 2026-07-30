namespace MediaWatcher;

public sealed class CommandResultEvent
{
    public string Event { get; init; } = "commandResult";

    public string? Id { get; init; }

    public string Command { get; init; } = "";

    public bool Success { get; init; }

    public string? Error { get; init; }
}
