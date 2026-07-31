using System.Text.Json;
using System.Text.Json.Serialization;
using MediaWatcher.Audio;
using MediaWatcher.Events;
using MediaWatcher.IO;
using MediaWatcher.Session;

namespace MediaWatcher.Commands;

public sealed class MediaCommandWatcher
{
    private readonly bool debugEnabled;

    private readonly JsonLineWriter output;

    private readonly Dictionary<string, Func<Task<bool>>> handlers;

    private readonly Dictionary<string, Func<float, Task<bool>>> valueHandlers;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MediaCommandWatcher(
        MediaSessionWatcher mediaSessionWatcher,
        AudioController audioController,
        JsonLineWriter output,
        bool debugEnabled = false
    )
    {
        this.output = output;
        this.debugEnabled = debugEnabled;

        handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // GSMTC
            ["play"] = mediaSessionWatcher.PlayAsync,
            ["pause"] = mediaSessionWatcher.PauseAsync,
            ["toggleplaypause"] = mediaSessionWatcher.TogglePlayPauseAsync,
            ["next"] = mediaSessionWatcher.NextAsync,
            ["previous"] = mediaSessionWatcher.PreviousAsync,
            ["prev"] = mediaSessionWatcher.PreviousAsync,
            ["stop"] = mediaSessionWatcher.StopAsync,

            // Core Audio
            ["mute"] = audioController.MuteAsync,
            ["unmute"] = audioController.UnmuteAsync,
            ["togglemute"] = audioController.ToggleMuteAsync,
            ["vol+"] = audioController.VolumeUpAsync,
            ["vol-"] = audioController.VolumeDownAsync,
        };

        valueHandlers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["setvolume"] = audioController.SetVolumeAsync,
        };
    }

    public async Task StartAsync()
    {
        while (await Console.In.ReadLineAsync() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            DebugLog($"stdin: {line}");

            MediaCommand? command;

            try
            {
                command = JsonSerializer.Deserialize<MediaCommand>(line, jsonOptions);
            }
            catch (Exception ex)
            {
                DebugLog($"Invalid JSON: {ex.Message}");
                WriteResult("", false);
                continue;
            }

            if (command == null || string.IsNullOrWhiteSpace(command.Command))
            {
                DebugLog("Missing command.");
                WriteResult(command?.Command ?? "", false);
                continue;
            }

            DebugLog($"Command='{command.Command}', Value={command.Value?.ToString() ?? "<null>"}");
            var success = await ExecuteAsync(command);
            WriteResult(command.Command, success);
        }
    }

    private async Task<bool> ExecuteAsync(MediaCommand command)
    {
        var name = command.Command?.Trim() ?? string.Empty;

        try
        {
            if (handlers.TryGetValue(name, out var handler))
            {
                DebugLog($"Executing '{name}'.");
                var result = await handler();
                DebugLog($"'{name}' returned {result}.");
                return result;
            }

            if (valueHandlers.TryGetValue(name, out var valueHandler))
            {
                if (!command.Value.HasValue)
                {
                    DebugLog($"'{name}' requires a value.");
                    return false;
                }

                DebugLog($"Executing '{name}' ({command.Value.Value}).");
                var result = await valueHandler(command.Value.Value);
                DebugLog($"'{name}' returned {result}.");
                return result;
            }
            DebugLog($"Unknown command '{name}'.");
        }
        catch (Exception ex)
        {
            DebugLog($"Exception while executing '{name}': {ex}");
        }

        return false;
    }

    private void WriteResult(string command, bool success)
    {
        output.Write(new CommandResultEvent { Command = command, Success = success });
    }

    private void DebugLog(string message)
    {
        if (!debugEnabled)
            return;

        Console.Error.WriteLine($"[MediaWatcher][command] {message}");
        Console.Error.Flush();
    }
}
