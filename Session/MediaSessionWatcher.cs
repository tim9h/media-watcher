using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using MediaWatcher.IO;
using Windows.Media.Control;

namespace MediaWatcher.Session;

public sealed class MediaSessionWatcher
{
    private readonly JsonLineWriter output;
    private readonly bool debugEnabled;

    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private GlobalSystemMediaTransportControlsSession? currentSession;

    private string? lastTitle;
    private string? lastArtist;
    private string? lastAlbum;
    private string? lastPlaybackState;

    public MediaSessionWatcher(JsonLineWriter output, bool debugEnabled = false)
    {
        this.output = output;
        this.debugEnabled = debugEnabled;
    }

    public async Task StartAsync()
    {
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        manager.CurrentSessionChanged += OnCurrentSessionChanged;

        var session = manager.GetCurrentSession();
        if (session == null)
        {
            Debug("No active media session.");
            return;
        }

        AttachSession(session);
        await PublishMedia(session);

        // Avoid emitting the initial playback state
        lastPlaybackState = session.GetPlaybackInfo().PlaybackStatus.ToString();
    }

    // ============================================================
    // GSMTC commands
    // ============================================================

    public Task<bool> PlayAsync() => ExecuteAsync(static s => s.TryPlayAsync().AsTask());

    public Task<bool> PauseAsync() => ExecuteAsync(static s => s.TryPauseAsync().AsTask());

    public Task<bool> TogglePlayPauseAsync() =>
        ExecuteAsync(static s => s.TryTogglePlayPauseAsync().AsTask());

    public Task<bool> NextAsync() => ExecuteAsync(static s => s.TrySkipNextAsync().AsTask());

    public Task<bool> PreviousAsync() =>
        ExecuteAsync(static s => s.TrySkipPreviousAsync().AsTask());

    public Task<bool> StopAsync() => ExecuteAsync(static s => s.TryStopAsync().AsTask());

    private async Task<bool> ExecuteAsync(
        Func<GlobalSystemMediaTransportControlsSession, Task<bool>> action
    )
    {
        var session = currentSession;
        if (session == null)
        {
            Debug("Command rejected: no active session.");
            return false;
        }

        try
        {
            return await action(session);
        }
        catch (Exception ex)
        {
            Debug($"GSMTC command failed: {ex.Message}");
            return false;
        }
    }

    // ============================================================
    // Session handling
    // ============================================================

    private void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args
    )
    {
        var session = sender.GetCurrentSession();
        if (session == null)
        {
            DetachSession();
            ResetCache();
            Debug("Current session removed.");
            return;
        }

        AttachSession(session);
        _ = PublishMedia(session);
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession session)
    {
        DetachSession();
        ResetCache();
        currentSession = session;
        session.MediaPropertiesChanged += OnMediaPropertiesChanged;
        session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        Debug($"Attached session: " + $"{Escape(session.SourceAppUserModelId)}");
    }

    private void DetachSession()
    {
        if (currentSession == null)
            return;

        currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        currentSession = null;
    }

    private void ResetCache()
    {
        (lastTitle, lastArtist, lastAlbum, lastPlaybackState) = (null, null, null, null);
    }

    // ============================================================
    // GSMTC events
    // ============================================================

    private void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args
    )
    {
        _ = PublishMedia(sender);
    }

    private void OnPlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args
    )
    {
        PublishPlayback(sender);
    }

    private async Task PublishMedia(GlobalSystemMediaTransportControlsSession session)
    {
        var media = await session.TryGetMediaPropertiesAsync();

        // Ignore late responses from old sessions
        if (!ReferenceEquals(session, currentSession))
        {
            return;
        }

        var title = CleanText(media.Title);
        var artist = CleanText(media.Artist);
        var album = CleanText(media.AlbumTitle);

        if (string.IsNullOrWhiteSpace(title))
            return;

        if (title == lastTitle && artist == lastArtist && album == lastAlbum)
        {
            return;
        }

        lastTitle = title;
        lastArtist = artist;
        lastAlbum = album;

        var playback = session.GetPlaybackInfo();

        lastPlaybackState = playback.PlaybackStatus.ToString();

        var (position, duration) = GetTimeline(session);

        output.Write(
            new MediaEvent
            {
                Event = "mediaChanged",

                Player = NormalizePlayer(session.SourceAppUserModelId),

                Title = title,
                Artist = artist,
                Album = album,

                State = lastPlaybackState,

                Position = position,
                Duration = duration,
            }
        );
    }

    private void PublishPlayback(GlobalSystemMediaTransportControlsSession session)
    {
        if (!ReferenceEquals(session, currentSession))
        {
            return;
        }

        var state = session.GetPlaybackInfo().PlaybackStatus.ToString();

        if (state == "Closed")
            return;

        if (state == lastPlaybackState)
            return;

        lastPlaybackState = state;

        var (position, duration) = GetTimeline(session);

        output.Write(
            new MediaEvent
            {
                Event = "playbackChanged",
                State = state,
                Position = position,
                Duration = duration,
            }
        );
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static (long Position, long Duration) GetTimeline(
        GlobalSystemMediaTransportControlsSession session
    )
    {
        var timeline = session.GetTimelineProperties();
        return (
            (long)timeline.Position.TotalMilliseconds,
            (long)timeline.EndTime.TotalMilliseconds
        );
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        return string.Join(
            " ",
            value.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
        );
    }

    private static string NormalizePlayer(string player)
    {
        if (string.IsNullOrWhiteSpace(player))
            return "";

        player = player.Trim();

        if (player.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            player = player[..^4];
        }

        if (player.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
        {
            return "Spotify";
        }

        return player;
    }

    private void Debug(string message)
    {
        if (!debugEnabled)
            return;

        Console.Error.WriteLine($"[MediaWatcher][session] {message}");
        Console.Error.Flush();
    }

    private static string Escape(string? value)
    {
        if (value == null)
            return "<null>";

        return value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}
