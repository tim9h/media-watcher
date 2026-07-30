using Windows.Media.Control;

namespace MediaWatcher;

public sealed class MediaSessionWatcher
{
    private readonly JsonLineWriter output;
    private readonly bool debugEnabled;

    private readonly object sessionLock = new();

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

    public GlobalSystemMediaTransportControlsSession? CurrentSession
    {
        get
        {
            lock (sessionLock)
            {
                return currentSession;
            }
        }
    }

    public async Task StartAsync()
    {
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        manager.CurrentSessionChanged += OnCurrentSessionChanged;

        var session = manager.GetCurrentSession();

        if (session == null)
        {
            DebugLog("No current media session available at startup.");
            return;
        }

        AttachSession(session);
        await PublishMedia(session);

        lastPlaybackState = session.GetPlaybackInfo().PlaybackStatus.ToString();
    }

    private void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args
    )
    {
        var session = sender.GetCurrentSession();

        if (session == null)
        {
            DetachCurrentSession();
            ResetCachedState();

            DebugLog("The current media session was removed.");
            return;
        }

        AttachSession(session);
        _ = PublishMedia(session);
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession session)
    {
        DetachCurrentSession();
        ResetCachedState();

        lock (sessionLock)
        {
            currentSession = session;

            currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;

            currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
        }

        DebugLog($"Attached session " + $"'{EscapeForLog(session.SourceAppUserModelId)}'.");
    }

    private void DetachCurrentSession()
    {
        lock (sessionLock)
        {
            if (currentSession == null)
                return;

            currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;

            currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;

            currentSession = null;
        }
    }

    private void ResetCachedState()
    {
        lastTitle = null;
        lastArtist = null;
        lastAlbum = null;
        lastPlaybackState = null;
    }

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
        try
        {
            var media = await session.TryGetMediaPropertiesAsync();

            // Ignore an async response from a session that is no longer
            // the active session.
            if (!ReferenceEquals(session, CurrentSession))
            {
                DebugLog("Ignoring metadata from an inactive session.");

                return;
            }

            var title = CleanText(media.Title);
            var artist = CleanText(media.Artist);
            var album = CleanText(media.AlbumTitle);

            DebugLog(
                $"raw metadata: title='{EscapeForLog(media.Title)}' "
                    + $"artist='{EscapeForLog(media.Artist)}' "
                    + $"album='{EscapeForLog(media.AlbumTitle)}' "
                    + $"source='{EscapeForLog(session.SourceAppUserModelId)}'"
            );

            DebugLog(
                $"normalized metadata: title='{EscapeForLog(title)}' "
                    + $"artist='{EscapeForLog(artist)}' "
                    + $"album='{EscapeForLog(album)}'"
            );

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
            var timeline = session.GetTimelineProperties();

            var state = playback.PlaybackStatus.ToString();

            lastPlaybackState = state;

            output.Write(
                new MediaEvent
                {
                    Event = "mediaChanged",
                    Player = NormalizePlayer(session.SourceAppUserModelId),
                    Title = title,
                    Artist = artist,
                    Album = album,
                    State = state,
                    Position = (long)timeline.Position.TotalMilliseconds,
                    Duration = (long)timeline.EndTime.TotalMilliseconds,
                }
            );
        }
        catch (Exception exception)
        {
            DebugLog(
                $"Publishing media failed: " + $"{exception.GetType().Name}: {exception.Message}"
            );
        }
    }

    private void PublishPlayback(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            if (!ReferenceEquals(session, CurrentSession))
                return;

            var playback = session.GetPlaybackInfo();
            var state = playback.PlaybackStatus.ToString();

            if (state == "Closed")
                return;

            if (state == lastPlaybackState)
                return;

            lastPlaybackState = state;

            var timeline = session.GetTimelineProperties();

            output.Write(
                new MediaEvent
                {
                    Event = "playbackChanged",
                    State = state,
                    Position = (long)timeline.Position.TotalMilliseconds,
                    Duration = (long)timeline.EndTime.TotalMilliseconds,
                }
            );
        }
        catch (Exception exception)
        {
            DebugLog(
                $"Publishing playback state failed: "
                    + $"{exception.GetType().Name}: {exception.Message}"
            );
        }
    }

    private void DebugLog(string message)
    {
        if (!debugEnabled)
            return;

        Console.Error.WriteLine($"[MediaWatcher][session][debug] {message}");

        Console.Error.Flush();
    }

    private static string EscapeForLog(string? value)
    {
        if (value is null)
            return "<null>";

        return value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
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
}
