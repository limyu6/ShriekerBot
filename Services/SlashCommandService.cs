using Discord;
using Discord.WebSocket;

namespace ShriekerBot.Services;

internal class SlashCommandService(
    ILogger<DiscordBotHost> logger,
    IConfiguration config,
    DiscordSocketClient client,
    WakeService wakeService)
{
    internal void Initialize()
    {
        client.Ready += ClientReady;
        client.SlashCommandExecuted += SlashCommandHandler;
        client.MessageReceived += OnMessageRecieved;
        logger.LogInformation("SlashCommandService initialized.");
    }
    private async Task SlashCommandHandler(SocketInteraction interaction)
    {
        if (interaction is not SocketSlashCommand command) return;

        switch (command.CommandName) //Add commands here (1/2)
        {
            case "wake":
                await wakeService.HandleWakeAsync(command);
                break;
            default:
                await command.RespondAsync($"Unknown command: {command.CommandName}");
                break;
        }

    }

    private async Task ClientReady()
    {
        var guildIds = config.GetSection("Discord:GuildIds").Get<ulong[]>();

        if (guildIds == null || guildIds.Length == 0)
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

    private async Task OnMessageRecieved(SocketMessage msg)
    {
        if (msg.Author.IsBot) return;
        logger.LogInformation($"{msg.Author.Username}: {msg.Content}");
        if (msg.Content == "!wake")
            await msg.Channel.SendMessageAsync("Waking linked PC...");
    }
}
