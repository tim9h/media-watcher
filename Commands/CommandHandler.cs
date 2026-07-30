namespace MediaWatcher.Commands;

public delegate Task<bool> CommandHandler(MediaCommand command);
