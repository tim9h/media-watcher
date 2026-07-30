# Media Watcher

Media Watcher is a lightweight Global System Media Transport Controls (GSMTC) media watcher written in C#. It writes playback events as JSON lines to `stdout` and accepts control commands as JSON lines via `stdin`.

> Targets Windows 10+ (build 19041 or later) via `net10.0-windows10.0.19041.0`.

## Player support

Works with any media player integrated with Windows System Media Transport Controls, including players such as Spotify, browsers, and supported MP3 players.

Some MP3 players don't support GSMTC out of the box and may require a plugin:

* [AIMP](https://aimp.ru/?do=catalog&rec_id=1097)
* [VLC](https://github.com/spmn/vlc-win10smtc)

## Usage

```bash
MediaWatcher.exe
```

Available command-line options:

| Option | Description |
|--------|-------------|
| `--version` | Prints the application version and exits. |
| `--debug` | Enables debug logging to `stderr`. JSON events continue to be written to `stdout`. |

## Output format

The application emits JSON objects for changes. Each event is written as a single JSON line.

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

Example `commandResult` event:

```json
{
  "event": "commandResult",
  "command": "next",
  "success": true
}
```

## Event fields

* `event` - `mediaChanged`, `playbackChanged` or `commandResult`
* `player` - normalized player identifier (may be empty for some sessions)
* `title` - current media title
* `artist` - current media artist
* `album` - current media album
* `state` - playback state, e.g. `Playing`, `Paused`
* `position` - current playback position in milliseconds (may be `0` if unsupported)
* `duration` - total duration in milliseconds (may be `0` if unsupported)
* `command` - executed command (`commandResult` only)
* `success` - whether the command was accepted (`commandResult` only)
* `error` - optional error code (`commandResult` only)

## Input commands

Media Watcher reads one JSON command per line from `stdin`.

Supported commands:

```json
{"command":"play"}
{"command":"pause"}
{"command":"togglePlayPause"}
{"command":"next"}
{"command":"previous"}
{"command":"stop"}
```

Each command produces a `commandResult` event indicating whether the command was accepted by the active media session.

## Notes

* `mediaChanged` is emitted when the current session media metadata changes.
* `playbackChanged` is emitted when playback state changes.
* `commandResult` is emitted after a command received on `stdin` has been processed.
* Debug logging is written to `stderr` when `--debug` is enabled.
* The output is JSON-serialized with camelCase property names.

## Building

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false
```