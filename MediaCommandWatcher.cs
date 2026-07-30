using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Media.Control;

namespace MediaWatcher;

public sealed class MediaCommandWatcher
{
    private readonly MediaSessionWatcher mediaSessionWatcher;
    private readonly JsonLineWriter output;
    private readonly bool debugEnabled;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public MediaCommandWatcher(
        MediaSessionWatcher mediaSessionWatcher,
        JsonLineWriter output,
        bool debugEnabled = false
    )
    {
        this.mediaSessionWatcher = mediaSessionWatcher;
        this.output = output;
        this.debugEnabled = debugEnabled;
    }

    public async Task RunAsync()
    {
        DebugLog("Command listener started.");

        while (true)
        {
            string? line;

            try
            {
                line = await Console.In.ReadLineAsync();
            }
            catch (Exception exception)
            {
                DebugLog(
                    $"Reading stdin failed: " + $"{exception.GetType().Name}: {exception.Message}"
                );

                return;
            }

            // Java closed the helper's stdin.
            if (line == null)
            {
                DebugLog("stdin was closed. Command listener is stopping.");
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            await ProcessLineAsync(line);
        }
    }

    private async Task ProcessLineAsync(string line)
    {
        MediaCommand? command;

        try
        {
            command = JsonSerializer.Deserialize<MediaCommand>(line, jsonOptions);
        }
        catch (JsonException exception)
        {
            DebugLog($"Invalid command JSON: '{EscapeForLog(line)}'; " + exception.Message);
            WriteResult(id: null, command: "", success: false, error: "invalidJson");
            return;
        }

        var commandName = command?.Command?.Trim();

        if (string.IsNullOrWhiteSpace(commandName))
        {
            WriteResult(id: command?.Id, command: "", success: false, error: "missingCommand");
            return;
        }

        DebugLog(
            $"Received command '{EscapeForLog(commandName)}' "
                + $"with id '{EscapeForLog(command?.Id)}'."
        );

        await ExecuteAsync(command!.Id, commandName);
    }

    private async Task ExecuteAsync(string? id, string command)
    {
        // Capture the reference once. The active session may switch while
        // the asynchronous GSMTC command is running.
        GlobalSystemMediaTransportControlsSession? session = mediaSessionWatcher.CurrentSession;

        if (session == null)
        {
            WriteResult(id, command, success: false, error: "noActiveSession");
            return;
        }

        try
        {
            bool success;
            switch (command.ToLowerInvariant())
            {
                case "play":
                    success = await session.TryPlayAsync();
                    break;

                case "pause":
                    success = await session.TryPauseAsync();
                    break;

                case "toggleplaypause":
                    success = await session.TryTogglePlayPauseAsync();
                    break;

                case "next":
                    success = await session.TrySkipNextAsync();
                    break;

                case "previous":
                case "prev":
                    success = await session.TrySkipPreviousAsync();
                    break;

                case "stop":
                    success = await session.TryStopAsync();
                    break;

                default:
                    WriteResult(id, command, success: false, error: "unknownCommand");
                    return;
            }
            WriteResult(id, command, success, success ? null : "commandRejected");
        }
        catch (Exception exception)
        {
            DebugLog(
                $"Command '{EscapeForLog(command)}' failed: "
                    + $"{exception.GetType().Name}: {exception.Message}"
            );

            WriteResult(id, command, success: false, error: "commandFailed");
        }
    }

    private void WriteResult(string? id, string command, bool success, string? error)
    {
        output.Write(
            new CommandResultEvent
            {
                Id = id,
                Command = command,
                Success = success,
                Error = error,
            }
        );
    }

    private void DebugLog(string message)
    {
        if (!debugEnabled)
            return;

        Console.Error.WriteLine($"[MediaWatcher][command][debug] {message}");
        Console.Error.Flush();
    }

    private static string EscapeForLog(string? value)
    {
        if (value is null)
            return "<null>";

        return value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}
