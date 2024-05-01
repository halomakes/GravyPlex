using CircularBuffer;
using DSharpPlus;
using DSharpPlus.Entities;
using Plex.Webhooks;

namespace GravyPlex.Handlers;

public class MediaAddedHandler(DiscordClient discord) : IPlexWebhookHandler
{
    private static readonly CircularBuffer<string> RecentTitles = new(5);

    public async Task Handle(PlexEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload is { Metadata: null } or
            { Event: not PlexEvents.Library.NewMedia and not PlexEvents.Library.OnDeck })
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
        var title = GetTitle(payload.Metadata);
        if (RecentTitles.Contains(title)) // prevent repetitive notification
            return;
        RecentTitles.PushFront(title);
        var channel = await discord.GetChannelAsync(1234283100947746827);
        var messageBuilder = new DiscordMessageBuilder()
            .WithContent(BuildContent(payload));
        messageBuilder.AddFile($"{payload.Metadata.Title}-thumb.jpg", thumbnail, AddFileOptions.None);
        await messageBuilder.SendAsync(channel);
    }

    private static string BuildContent(PlexEventPayload payload)
    {
        var metadata = payload.Metadata!;
        var headerLine = $"## {GetTitle(metadata)}";
        var typeLine = $"**New {metadata.Type} Added**";
        var descriptionLine = metadata.Summary;
        var link = payload.GenerateMediaLink();
        var linkLine = link is null ? null : $"[Watch it here]({link})";
        List<string?> lines = [headerLine, typeLine, linkLine, descriptionLine];
        var combined = string.Join(Environment.NewLine, lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        return combined;
    }

    private static string GetTitle(MediaMetadata metadata)
    {
        List<string?> headerSegments = [metadata.GrandparentTitle, metadata.ParentTitle, metadata.Title];
        return string.Join(" › ", headerSegments.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}