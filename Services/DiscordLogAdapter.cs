using Discord;
using Discord.WebSocket;

namespace ShriekerBot.Services;

internal class DiscordLogAdapter (
    ILogger<DiscordBotHost> logger,
    DiscordSocketClient client)
{
    internal void Initialize()
    {
        client.Log += LogAsync;
        logger.LogInformation("DiscordLogAdapter initialized.");
    }
    private Task LogAsync(LogMessage msg)
    {
        var severity = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };
        logger.Log(severity, msg.Exception, $"[Discord] {msg.Message}");
        return Task.CompletedTask;
    }
}
