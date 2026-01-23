namespace ShriekerBot;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? discordToken = configuration["Discord:Token"];
        if (string.IsNullOrEmpty(discordToken) || discordToken.Length < 10) 
        {
            logger.LogError("[Error] Discord Bot Token not found or Invalid Discord Bot Token! Please check secret.json(windows) or environment variable(linux).");
            return;
        }
        logger.LogInformation($"Discord Bot Token: {discordToken[..4]}************{discordToken[^4..]}");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                await Task.Delay(10000, stoppingToken);
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
    }
}
