using ShriekerBot;
using Discord;
using Discord.WebSocket;
using ShriekerBot.Services;

var builder = Host.CreateApplicationBuilder(args);

var socketConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
};

builder.Services.AddSingleton(new DiscordSocketClient(socketConfig));
builder.Services.AddSingleton<DiscordLogAdapter>();
builder.Services.AddSingleton<SlashCommandService>();
builder.Services.AddSingleton<WakeService>();
builder.Services.AddSingleton<ActiveService>();

builder.Services.AddHostedService<DiscordBotHost>();

var host = builder.Build();
host.Run();
