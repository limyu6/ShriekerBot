using Discord;
using Discord.WebSocket;
using ShriekerBot.Services;

namespace ShriekerBot;

internal class DiscordBotHost(
    ILogger<DiscordBotHost> logger, 
    IConfiguration config,
    DiscordSocketClient client,
    DiscordLogAdapter discordLogAdapter,
    SlashCommandService slashCommandService
    ) : BackgroundService
{
    private readonly string? _discordToken = config["Discord:Token"];
    //private readonly string _discordPrefix = config["Discord:Prefix"] ?? "!";

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_discordToken) || _discordToken.Length < 10) 
        {
            logger.LogError("[Error] Discord Bot Token not found or Invalid Discord Bot Token! Please check secret.json(windows) or environment variable(linux).");
            return;
        }
        logger.LogInformation($"Discord Bot Token: {_discordToken[..4]}************{_discordToken[^4..]}");

        try
        {
            discordLogAdapter.Initialize();
            slashCommandService.Initialize();

            logger.LogInformation("Logging in to Discord...");
            await client.LoginAsync(TokenType.Bot, _discordToken);

            logger.LogInformation("Starting Discord client...");
            await client.StartAsync();

            while (!cancellationToken.IsCancellationRequested) //start main logic here
            {
                //if (logger.IsEnabled(LogLevel.Information))
                //{
                //    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //}

                await Task.Delay(60000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("DiscordBotHost is stopping gracefully...");
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
}
