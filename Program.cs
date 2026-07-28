using System.Reflection;
using MediaWatcher;

if (args.Contains("--version"))
{
    var version = Assembly
        .GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
        ?? "unknown";

    Console.WriteLine($"MediaWatcher {version}");
    return;
}

var watcher = new MediaSessionWatcher();

await watcher.StartAsync();

await Task.Delay(Timeout.Infinite);