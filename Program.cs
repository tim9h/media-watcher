using System.Reflection;
using MediaWatcher.Audio;
using MediaWatcher.Commands;
using MediaWatcher.IO;
using MediaWatcher.Session;

if (args.Contains("--version", StringComparer.OrdinalIgnoreCase))
{
    var version =
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "unknown";

    Console.WriteLine($"MediaWatcher {version}");
    return;
}

var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

var writer = new JsonLineWriter();

var sessionWatcher = new MediaSessionWatcher(writer, debugEnabled);

await sessionWatcher.StartAsync();

var audioController = new AudioController(writer);

var commandWatcher = new MediaCommandWatcher(sessionWatcher, audioController, writer, debugEnabled);

await commandWatcher.StartAsync();
