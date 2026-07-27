namespace MediaWatcher;

public class MediaEvent
{
    public string Event { get; set; } = "";

    public string? Player { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? Album { get; set; }

    public string? State { get; set; }

    public long? Position { get; set; }

    public long? Duration { get; set; }
}
