namespace TenisDeMasa.Worker.Application.Discord;

using Microsoft.Extensions.Configuration;

public class DiscordGuild(
    ILogger<DiscordGuild> logger,
    DiscordRestClient client,
    IConfiguration configuration)
{
    public async Task SendMessageAsync(string message, string channels)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogInformation("Empty message {message}", message);
            return;
        }

        string? token = configuration.GetConnectionString("DISCORD_TOKEN");

        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        await client.LoginAsync(TokenType.Bot, token);

        await ReadyAsync(message);
    }

    public async Task ReadyAsync(string message)
    {
        try
        {
            // TODO: use IOptions pattern
            // var discordGuildId = configuration.GetValue<ulong>("DiscordConfig:DiscordGuildId");
            // var guildChannels = await client.GetGuildAsync(discordGuildId);

            var discordAdminChannelId = configuration.GetValue<ulong>("DiscordConfig:DiscordAdminChannelId");

            ITextChannel channel =
                (ITextChannel)await client.GetChannelAsync(discordAdminChannelId);

            await channel.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
