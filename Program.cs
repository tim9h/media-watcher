using MediaWatcher;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var watcher = new MediaSessionWatcher();

await watcher.StartAsync();

await Task.Delay(Timeout.Infinite);
