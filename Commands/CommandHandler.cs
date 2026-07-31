namespace MediaBridge.Commands;

public delegate Task<bool> CommandHandler(MediaCommand command);
