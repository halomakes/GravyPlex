using DSharpPlus;
using DSharpPlus.Entities;
using Plex.Webhooks;

namespace GravyPlex.Handlers;

public class MediaAddedHandler(DiscordClient discord) : IPlexWebhookHandler
{
    public async Task Handle(PlexEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload is { Metadata: null } or { Event: not PlexEvents.Library.NewMedia and not PlexEvents.Library.OnDeck })
            return;
        var channel = await discord.GetChannelAsync(1234283100947746827);
        var messageBuilder = new DiscordMessageBuilder()
            .WithContent(BuildContent(payload));
        await messageBuilder.SendAsync(channel);
    }

    public async Task Handle(PlexEventPayload payload, Stream thumbnail, CancellationToken cancellationToken)
    {
        if (payload is { Metadata: null } or { Event: not PlexEvents.Library.NewMedia })
            return;
        var channel = await discord.GetChannelAsync(1234283100947746827);
        var messageBuilder = new DiscordMessageBuilder()
            .WithContent(BuildContent(payload));
        messageBuilder.AddFile($"{payload.Metadata.Title}-thumb.jpg", thumbnail, AddFileOptions.None);
        await messageBuilder.SendAsync(channel);
    }

    private string BuildContent(PlexEventPayload payload)
    {
        var metadata = payload.Metadata!;
        List<string?> headerSegments = [metadata.GrandparentTitle, metadata.ParentTitle, metadata.Title];
        var headerLine = $"## {string.Join(" › ", headerSegments.Where(s => !string.IsNullOrWhiteSpace(s)))}";
        var typeLine = $"**New {metadata.Type} Added**";
        var descriptionLine = metadata.Summary;
        var link = payload.GenerateMediaLink();
        var linkLine = link is null ? null : $"[Watch it here]({link})";
        List<string?> lines = [headerLine, typeLine, linkLine, descriptionLine];
        var combined = string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        return combined;
    }
}