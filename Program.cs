using ShriekerBot;
using Discord;
using Discord.WebSocket;

var builder = Host.CreateApplicationBuilder(args);

var socketConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
};

builder.Services.AddSingleton(new DiscordSocketClient(socketConfig));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
