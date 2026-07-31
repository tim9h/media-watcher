using System.Reflection;
using MediaBridge.Audio;
using MediaBridge.Commands;
using MediaBridge.IO;
using MediaBridge.Session;

if (args.Contains("--version", StringComparer.OrdinalIgnoreCase))
{
    var version =
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "unknown";

    Console.WriteLine($"MediaBridge {version}");
    return;
}

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

var writer = new JsonLineWriter();

var sessionWatcher = new MediaSessionWatcher(writer, debugEnabled);

await sessionWatcher.StartAsync();

var audioController = new AudioController(writer);

var commandWatcher = new MediaCommandWatcher(sessionWatcher, audioController, writer, debugEnabled);

await commandWatcher.StartAsync();
