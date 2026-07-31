namespace MediaBridge.Events;

public sealed class VolumeChangedEvent
{
    public string Event { get; init; } = "volumeChanged";

    public float Volume { get; init; }

    public bool Muted { get; init; }
}
