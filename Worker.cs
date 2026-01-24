using Discord;
using Discord.WebSocket;

namespace ShriekerBot;

public class Worker(
    ILogger<Worker> logger, 
    IConfiguration config,
    DiscordSocketClient client
    ) : BackgroundService
{
    private readonly string? _discordToken = config["Discord:Token"];
    private readonly string _discordPrefix = config["Discord:Prefix"] ?? "!";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_discordToken) || _discordToken.Length < 10) 
        {
            logger.LogError("[Error] Discord Bot Token not found or Invalid Discord Bot Token! Please check secret.json(windows) or environment variable(linux).");
            return;
        }
        logger.LogInformation($"Discord Bot Token: {_discordToken[..4]}************{_discordToken[^4..]}");

        try
        {
            client.Log += LogAsync;

            client.MessageReceived += OnMessageRecieved;

            logger.LogInformation("Logging in to Discord...");
            await client.LoginAsync(TokenType.Bot, _discordToken);

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

    private async Task OnMessageRecieved(SocketMessage msg)
    {
        if (msg.Author.IsBot) return;
        if (msg.Channel.Name != "機器人測試") return;
        if (!msg.Content.StartsWith(_discordPrefix)) return;
        string command = msg.Content[_discordPrefix.Length..].Trim().ToLower();
        switch (command)
        {
            case "wake":
                await msg.Channel.SendMessageAsync("Waking linked PC...");
                break;
            case "晚安":
                await msg.Channel.SendMessageAsync("晚安，瑪卡巴卡！");
                break;
            default:
                await msg.Channel.SendMessageAsync($"Unknown command: {command}");
                break;
        }
        
    }
}
