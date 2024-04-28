using System.Text.Json;
using System.Text.Json.Serialization;
using DSharpPlus;
using DSharpPlus.Entities;
using Plex.Webhooks;

namespace GravyPlex.Handlers;

public class MediaAddedHandler(DiscordClient discord) : IPlexWebhookHandler
{
    public async Task Handle(PlexEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload is { Metadata: null } or { Event: not PlexEvents.Library.NewMedia })
            return;
        var channel = await discord.GetChannelAsync(1220589799711445012);
        var messageBuilder = new DiscordMessageBuilder()
            .WithContent($"""
                          ## {payload.Metadata.Title}
                          **New {payload.Metadata.Type} Added**
                          """);
        if (payload.Metadata.Thumbnail is not null)
        {
            var url = new Uri(new Uri("https://plex.gravy.network/web"), payload.Metadata.Thumbnail);
            messageBuilder.AddEmbed(new DiscordEmbedBuilder()
                .WithTitle(payload.Metadata.Title)
                .WithImageUrl(url)
            );
        }
        await messageBuilder.SendAsync(channel);
        
        //debug
        await new DiscordMessageBuilder()
            .WithContent(JsonSerializer.Serialize(payload))
            .SendAsync(channel);
    }
}