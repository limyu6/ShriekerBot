using Discord.WebSocket;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ShriekerBot.Services;

internal class WakeService(
    ILogger<DiscordBotHost> logger,
    IConfiguration config)
{
    internal async Task HandleWakeAsync(SocketSlashCommand command)
    {
        logger.LogInformation($"{command.User.Username} used /wake.");
        await command.RespondAsync("🔍 Checking PC status...");
        try
        {
            string macIp = command.Data.Options
                .FirstOrDefault(x => x.Name == "mac")?
                .Value.ToString()
                ?? config["WoL:MacIp"]!;
            string? targetIp = config["WoL:TargetIp"];

            if (!string.IsNullOrEmpty(targetIp) && await PingHostAsync(targetIp))
            {
                logger.LogWarning("PC is already Online!");
                await command.ModifyOriginalResponseAsync(msg => msg.Content = "⚠️ PC is already Online!");
                return;
            }

            await SendMagicPacketAsync(macIp, targetIp);
            await command.ModifyOriginalResponseAsync(msg => msg.Content = "🚀 Magic Packet sent!");

            _ = Task.Run(async () =>
            {
                try
                {
                    await VerifyPcWakeUpAsync(command, targetIp);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background verification failed.");
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError($"[Wake] {ex.Message}");
            await command.ModifyOriginalResponseAsync(msg => msg.Content = $"❌ Error: {ex.Message}");
        }
    }
    private async Task SendMagicPacketAsync(string macAddress, string? targetIp)
    {
        var macBytes = System.Net.NetworkInformation.PhysicalAddress
            .Parse(macAddress.Replace(":", "").Replace("-", ""))
            .GetAddressBytes();

        //Length of Magic Packet = 6 (FF) + 16 * 6 (MAC) = 102 bytes
        var packet = new byte[102];

        for (int i = 0; i < 6; i++)
            packet[i] = 0xFF;

        for (int i = 0; i < 16; i++)
            Array.Copy(macBytes, 0, packet, 6 + i * 6, 6);

        using var client = new UdpClient();
        client.EnableBroadcast = true;
        if (string.IsNullOrEmpty(targetIp))
        {
            await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));
            return;
        }
        string broadcastIp = targetIp.Substring(0, targetIp.LastIndexOf('.') + 1) + "255";
        logger.LogInformation($"Sending Magic Packet to {broadcastIp}...");
        await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Parse(broadcastIp), 9));
    }

    private async Task VerifyPcWakeUpAsync(SocketSlashCommand command, string? targetIp)
    {
        await command.ModifyOriginalResponseAsync(msg => msg.Content = "Computer should wake up in few seconds...");
        if (string.IsNullOrEmpty(targetIp))
        {
            await command.ModifyOriginalResponseAsync(msg => msg.Content = "🚀 Magic Packet sent!");
            await command.FollowupAsync("Skipped verification: No IP configured");
            return;
        }
        bool isWaked = false;
        for (int i = 0; i < 120; i++)
        {
            if (await PingHostAsync(targetIp))
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

    private async Task<bool> PingHostAsync(string targetIp)
    {
        using var pinger = new Ping();
        try
        {
            var reply = await pinger.SendPingAsync(targetIp, 1000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
