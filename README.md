# Media Watcher

Media Watcher is a lightweight Global System Media Transport Controls (GSMTC) media watcher written in C#. It writes playback events as JSON lines to `stdout`.

> Targets Windows 10+ (build 19041 or later) via `net10.0-windows10.0.19041.0`.

## Player support
Works with any media player integrated with Windows system media transport controls, including players such as Spotify, browsers, and supported MP3 players.

Some MP3 players don't support GSMTC out of the box and may require a plugin:
* [AIMP](https://aimp.ru/?do=catalog&rec_id=1097)
* [VLC](https://github.com/spmn/vlc-win10smtc)

## Output format
The app emits JSON objects for changes. Each event is written as a single JSON line.

Example `mediaChanged` event:
```json
{
  "event": "mediaChanged",
  "player": "Spotify",
  "title": "Song Title",
  "artist": "Artist Name",
  "album": "Album Name",
  "state": "Playing",
  "position": 12345,
  "duration": 234567
}
```

Example `playbackChanged` event:
```json
{
  "event": "playbackChanged",
  "state": "Paused",
  "position": 12345,
  "duration": 234567
}
```

## Event fields
* `event` - `mediaChanged` or `playbackChanged`
* `player` - normalized player identifier (may be empty for some sessions)
* `title` - current media title
* `artist` - current media artist
* `album` - current media album
* `state` - playback state, e.g. `Playing`, `Paused`
* `position` - current playback position in milliseconds (may be `0` if unsupported)
* `duration` - total duration in milliseconds (may be `0` if unsupported)

## Notes
* `mediaChanged` is emitted when the current session media metadata changes.
* `playbackChanged` is emitted when playback state changes.
* The output is JSON-serialized with camelCase property names.


## Building

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false
```