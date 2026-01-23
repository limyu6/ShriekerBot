using Discord;
using Discord.WebSocket;

namespace ShriekerBot;

public class Worker(
    ILogger<Worker> logger, 
    IConfiguration config,
    DiscordSocketClient client
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? discordToken = config["Discord:Token"];
        string? discordPrefix = config["Discord:Prefix"] ?? "!";
        if (string.IsNullOrEmpty(discordToken) || discordToken.Length < 10) 
        {
            logger.LogError("[Error] Discord Bot Token not found or Invalid Discord Bot Token! Please check secret.json(windows) or environment variable(linux).");
            return;
        }
        logger.LogInformation($"Discord Bot Token: {discordToken[..4]}************{discordToken[^4..]}");

        try
        {
            client.Log += LogAsync;

            logger.LogInformation("Logging in to Discord...");
            await client.LoginAsync(TokenType.Bot, discordToken);

            logger.LogInformation("Starting Discord client...");
            await client.StartAsync();

            while (!stoppingToken.IsCancellationRequested) //start main logic here
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                await Task.Delay(60000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Worker is stopping gracefully...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occured!");
            throw;
        }
        finally
        {
            logger.LogInformation("Stopping Discord client...");
            await client.StopAsync();
        }
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
        logger.Log(severity, msg.Exception, "[Discord] {Message}", msg.Message);
        return Task.CompletedTask;
    }
}
