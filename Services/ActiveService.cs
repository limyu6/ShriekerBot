using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShriekerBot.Services;

internal class ActiveService(
    ILogger<DiscordBotHost> logger,
    IConfiguration config,
    WakeService wakeService)
{
    private RestUserMessage? _lastMessage;
    internal async Task HandleActiveAsync(SocketSlashCommand command)
    {
        string? serverIp = command.Data.Options
                .FirstOrDefault(x => x.Name == "sip")?
                .Value.ToString()
                ?? config["MCServer:ServerIp"];
        await command.RespondAsync("Waking PC...");
        _lastMessage = await command.GetOriginalResponseAsync();
        await wakeService.HandleWakeAsync(command, _lastMessage);
        _lastMessage = await command.Channel.SendMessageAsync(text: $"Activating minecraft server with ip: {serverIp}");
        //Activate logic...
    }
}
