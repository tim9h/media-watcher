using System.Text.Json;
using MediaWatcher.Audio;
using MediaWatcher.IO;
using MediaWatcher.Session;

namespace MediaWatcher.Commands;

public sealed class MediaCommandWatcher
{
    private readonly AudioController audioController;
    private readonly JsonLineWriter output;
    private readonly bool debugEnabled;

    private readonly Dictionary<string, Func<Task<bool>>> handlers;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public MediaCommandWatcher(
        MediaSessionWatcher mediaSessionWatcher,
        AudioController audioController,
        JsonLineWriter output,
        bool debugEnabled = false
    )
    {
        this.audioController = audioController;
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
    }

    public async Task RunAsync()
    {
        Debug("Command listener started.");

        while (true)
        {
            string? line;

            try
            {
                line = await Console.In.ReadLineAsync();
            }
            catch (Exception ex)
            {
                Debug($"stdin error: {ex.Message}");
                return;
            }

            if (line == null)
            {
                Debug("stdin closed.");
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
        catch
        {
            WriteResult("", false, "invalidJson");
            return;
        }

        if (command == null || string.IsNullOrWhiteSpace(command.Command))
        {
            WriteResult("", false, "missingCommand");
            return;
        }

        if (!handlers.TryGetValue(command.Command, out var handler))
        {
            WriteResult(command.Command, false, "unknownCommand");
            return;
        }

        try
        {
            bool success = await handler();
            WriteResult(command.Command, success, success ? null : "commandRejected");
        }
        catch (Exception ex)
        {
            Debug(ex.ToString());
            WriteResult(command.Command, false, "commandFailed");
        }
    }

    private void WriteResult(string command, bool success, string? error)
    {
        output.Write(
            new CommandResultEvent
            {
                Command = command,
                Success = success,
                Error = error,
            }
        );
    }

    private void Debug(string message)
    {
        if (!debugEnabled)
            return;

        Console.Error.WriteLine($"[MediaWatcher][command] {message}");
        Console.Error.Flush();
    }
}
