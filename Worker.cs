using Discord;
using Discord.WebSocket;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace ShriekerBot;

public class Worker(
    ILogger<Worker> logger, 
    IConfiguration config,
    DiscordSocketClient client
    ) : BackgroundService
{
    private readonly string? _discordToken = config["Discord:Token"];
    //private readonly string _discordPrefix = config["Discord:Prefix"] ?? "!";

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
            client.InteractionCreated += SlashCommandHandler;
            client.Ready += ClientReady;

            logger.LogInformation("Logging in to Discord...");
            await client.LoginAsync(TokenType.Bot, _discordToken);

            logger.LogInformation("Starting Discord client...");
            await client.StartAsync();

            while (!stoppingToken.IsCancellationRequested) //start main logic here
            {
                //if (logger.IsEnabled(LogLevel.Information))
                //{
                //    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //}

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

    private async Task SlashCommandHandler(SocketInteraction interaction)
    {
        if (interaction is not SocketSlashCommand command) return;
        
        switch (command.CommandName) //Add commands here (1/2)
        {
            case "wake":
                logger.LogInformation($"{command.User.Username} used /wake.");
                await command.RespondAsync("🔍 Checking PC status...");
                try
                {
                    string macIp = command.Data.Options
                        .FirstOrDefault(x => x.Name == "mac")?
                        .Value.ToString()
                        ?? config["WoL:MacIp"]!;
                    string? targetIp = config["WoL:TargetIp"];
                    if (!string.IsNullOrEmpty(targetIp) && await PingHost(targetIp)) 
                    {
                        logger.LogWarning("PC is already Online!");
                        await command.ModifyOriginalResponseAsync(msg => msg.Content = "⚠️ PC is already Online!");
                        return;
                    }

                    await SendWakeOnLan(macIp);
                    await command.ModifyOriginalResponseAsync(msg => msg.Content = "🚀 Magic Packet sent!");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await VerifyPcWakeUp(command, targetIp);
                        }
                        catch(Exception ex)
                        {
                            logger.LogError(ex, "Background verification failed.");
                        }
                    });
                }
                catch(Exception ex) 
                {
                    logger.LogError($"[Wake] {ex.Message}");
                    await command.ModifyOriginalResponseAsync(msg => msg.Content = $"❌ Error: {ex.Message}");
                }
                break;
            default:
                await command.RespondAsync($"Unknown command: {command.CommandName}");
                break;
        }
        
    }

    private async Task ClientReady()
    {
        var guildIds = config.GetSection("Discord:GuildIds").Get<ulong[]>();

        if(guildIds == null || guildIds.Length == 0)
        {
            logger.LogWarning("No Guild IDs configured for slash commands registration.");
            return;
        }

        var slashCommands = new List<SlashCommandProperties>(); //Add commands here (2/2)
        slashCommands.Add(new SlashCommandBuilder()
            .WithName("wake")
            .WithDescription("Wake the linked PC")
            .AddOption("mac", ApplicationCommandOptionType.String, "The MAC address to wake", isRequired: false)
            .Build());

        foreach (var guildId in guildIds)
        {
            try
            {
                var guild = client.GetGuild(guildId);
                if (guild == null)
                {
                    logger.LogWarning($"Guild ID {guildId} not found. Is the bot in this server?");
                    continue;
                }
                
                await guild.BulkOverwriteApplicationCommandAsync(slashCommands.ToArray());
                logger.LogInformation($"Registered slash commands for guild: {guild.Name}({guildId})");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to register slash commands for guild ID: {guildId}");
            }
        }
    }

    private async Task SendWakeOnLan(string macAddress)
    {
        var macBytes = System.Net.NetworkInformation.PhysicalAddress
            .Parse(macAddress.Replace(":","").Replace("-",""))
            .GetAddressBytes();

        //Length of Magic Packet = 6 (FF) + 16 * 6 (MAC) = 102 bytes
        var packet = new byte[102];

        for (int i = 0; i < 6; i++)
            packet[i] = 0xFF;

        for (int i = 0; i < 16; i++) 
            Array.Copy(macBytes, 0, packet, 6 + i * 6, 6);

        using var client = new UdpClient();
        client.EnableBroadcast = true;
        await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));
    }
    
    private async Task VerifyPcWakeUp(SocketSlashCommand command, string? targetIp)
    {
        await command.ModifyOriginalResponseAsync(msg => msg.Content = "Computer should wake up in few seconds...");
        if (string.IsNullOrEmpty(targetIp))
        {
            await command.ModifyOriginalResponseAsync(msg => msg.Content = "Skipped verification: No IP configured");
            return;
        }
        bool isWaked = false;
        for (int i = 0; i < 120; i++)
        {
            if (await PingHost(targetIp))
            {
                isWaked = true;
                break;
            }
            if (i % 3 == 0)
                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Computer should wake up in few seconds .   ({i / 2 + 1}/60s)");
            if (i % 3 == 1)
                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Computer should wake up in few seconds ..  ({i / 2 + 1}/60s)");
            if (i % 3 == 2)
                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Computer should wake up in few seconds ... ({i / 2 + 1}/60s)");
            await Task.Delay(500);
        }

        if (isWaked)
            await command.ModifyOriginalResponseAsync(msg => msg.Content = "✅ Success! PC is now Online!");
        else
            await command.ModifyOriginalResponseAsync(msg => msg.Content = "❌ Packet sent, but PC did not respond. Check BIOS/Network settings.");
    }

    private async Task<bool> PingHost(string targetIp)
    {
        using var pinger = new Ping();
        try
        {
            var reply = await pinger.SendPingAsync(targetIp, 100);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
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
        logger.Log(severity, msg.Exception, $"[Discord] {msg.Message}");
        return Task.CompletedTask;
    }

    private async Task OnMessageRecieved(SocketMessage msg)
    {
        if(msg.Author.IsBot) return;     
        logger.LogInformation($"{msg.Author.Username}: {msg.Content}");
        if(msg.Content=="!wake")
            await msg.Channel.SendMessageAsync("Waking linked PC...");
    }
}
