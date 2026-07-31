namespace MediaBridge.Commands;

public sealed class CommandResultEvent
{
    public string Event { get; init; } = "commandResult";

    public string Command { get; init; } = "";

    public bool Success { get; init; }
}
