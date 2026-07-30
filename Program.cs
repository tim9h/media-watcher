using System.Reflection;
using MediaWatcher;

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

var output = new JsonLineWriter();

var mediaSessionWatcher = new MediaSessionWatcher(output, debugEnabled);

await mediaSessionWatcher.StartAsync();

var mediaCommandWatcher = new MediaCommandWatcher(mediaSessionWatcher, output, debugEnabled);

// Keeps the process alive until Java closes stdin.
await mediaCommandWatcher.RunAsync();
