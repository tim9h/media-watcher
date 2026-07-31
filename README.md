# MediaBridge

MediaBridge is a lightweight Global System Media Transport Controls (GSMTC) media watcher written in C#. It writes playback and audio events as JSON lines to `stdout` and accepts control commands as JSON lines via `stdin`.

> Targets Windows 10+ (build 19041 or later) via `net10.0-windows10.0.19041.0`.

## Player support

Works with any media player integrated with Windows System Media Transport Controls, including players such as Spotify, browsers, and supported MP3 players.

Some MP3 players don't support GSMTC out of the box and may require a plugin:

* [AIMP](https://aimp.ru/?do=catalog&rec_id=1097)
* [VLC](https://github.com/spmn/vlc-win10smtc)

## Usage

```bash
MediaBridge.exe
```

Available command-line options:

| Option | Description |
|--------|-------------|
| `--version` | Prints the application version and exits. |
| `--debug` | Enables debug logging to `stderr`. JSON events continue to be written to `stdout`. |

## Output format

The application emits newline-delimited JSON (NDJSON). Each event is written as a single JSON object on its own line.

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

Example `volumeChanged` event:

```json
{
  "event": "volumeChanged",
  "volume": 0.42,
  "muted": false
}
```

Example `commandResult` event:

```json
{
  "event": "commandResult",
  "command": "next",
  "success": true
}
```

## Event fields

| Field | Description |
|-------|-------------|
| `event` | `mediaChanged`, `playbackChanged`, `volumeChanged` or `commandResult` |
| `player` | Normalized player identifier (`mediaChanged` only) |
| `title` | Current media title |
| `artist` | Current media artist |
| `album` | Current media album |
| `state` | Playback state (`Playing`, `Paused`, etc.) |
| `position` | Current playback position in milliseconds (may be `0` if unsupported) |
| `duration` | Total media duration in milliseconds (may be `0` if unsupported) |
| `volume` | Master system volume (`0.0` – `1.0`) |
| `muted` | Whether the default playback device is muted |
| `command` | Executed command (`commandResult` only) |
| `success` | Whether the command completed successfully (`commandResult` only) |

## Input commands

Media Watcher reads one JSON command per line from `stdin`.

### Media transport

```json
{"command":"play"}
{"command":"pause"}
{"command":"togglePlayPause"}
{"command":"next"}
{"command":"previous"}
{"command":"stop"}
```

### System audio

```json
{"command":"mute"}
{"command":"unmute"}
{"command":"toggleMute"}
{"command":"vol+"}
{"command":"vol-"}
{"command":"setVolume","value":0.5}
```

`setVolume` expects a floating-point value between `0.0` (0%) and `1.0` (100%).

Every command produces a `commandResult` event indicating whether it completed successfully.

Volume changes additionally emit a `volumeChanged` event. These events are generated for both commands and external system volume changes (for example, keyboard media keys or the Windows volume mixer).

## Notes

* `mediaChanged` is emitted when the current session media metadata changes.
* `playbackChanged` is emitted when playback state changes.
* `volumeChanged` is emitted whenever the system master volume or mute state changes.
* `commandResult` is emitted after a command received on `stdin` has been processed.
* Debug logging is written to `stderr` when `--debug` is enabled.
* JSON output uses camelCase property names.

## Building

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false
```