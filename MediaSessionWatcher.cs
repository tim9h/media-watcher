using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Media.Control;

namespace MediaWatcher;

public class MediaSessionWatcher
{
    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private GlobalSystemMediaTransportControlsSession? currentSession;

    private string? lastTitle;
    private string? lastArtist;
    private string? lastAlbum;
    private string? lastPlaybackState;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task StartAsync()
    {
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        manager.CurrentSessionChanged += OnCurrentSessionChanged;

        var session = manager.GetCurrentSession();

        if (session != null)
        {
            AttachSession(session);

            await PublishMedia(session);

            // Prevent the initial playbackChanged event from being emitted.
            lastPlaybackState = session.GetPlaybackInfo().PlaybackStatus.ToString();
        }
    }

    private void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args
    )
    {
        var session = sender.GetCurrentSession();

        if (session == null)
            return;

        AttachSession(session);

        _ = PublishMedia(session);
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession session)
    {
        if (currentSession != null)
        {
            currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        currentSession = session;

        currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
        currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
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
        var media = await session.TryGetMediaPropertiesAsync();

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
        var timeline = session.GetTimelineProperties();

        var state = playback.PlaybackStatus.ToString();

        lastPlaybackState = state;

        Write(
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

    private void PublishPlayback(GlobalSystemMediaTransportControlsSession session)
    {
        var playback = session.GetPlaybackInfo();
        var state = playback.PlaybackStatus.ToString();

        if (state == "Closed")
            return;

        if (state == lastPlaybackState)
            return;

        lastPlaybackState = state;

        var timeline = session.GetTimelineProperties();

        Write(
            new MediaEvent
            {
                Event = "playbackChanged",
                State = state,
                Position = (long)timeline.Position.TotalMilliseconds,
                Duration = (long)timeline.EndTime.TotalMilliseconds,
            }
        );
    }

    private void Write(MediaEvent e)
    {
        Console.WriteLine(JsonSerializer.Serialize(e, jsonOptions));
        Console.Out.Flush();
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
            player = player[..^4];

        // Windows Store apps often report "...SpotifyMusic..."
        if (player.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
            return "Spotify";

        return player;
    }
}
