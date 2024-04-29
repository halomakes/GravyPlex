using GravyPlex;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Plex.Webhooks;

public static class PlexEvents
{
    public static class Library
    {
        public const string OnDeck = "library.on.deck";
        public const string NewMedia = "library.new";
    }

    public static class Media
    {
        public const string Pause = "media.pause";
        public const string Play = "media.play";
        public const string Rate = "media.rate";
        public const string Resume = "media.resume";
        public const string Scrobble = "media.scrobble";
        public const string Stop = "media.stop";
    }

    public static class Admin
    {
        public const string DatabaseBackup = "admin.database.backup";
        public const string DatabaseCorrupted = "admin.database.corrupted";
        public const string NewDevice = "device.new";
        public const string PlaybackStarted = "playback.started";
    }
}

public class PlexEventPayload
{
    public required string Event { get; init; }

    [JsonPropertyName("user")]
    public bool IsUser { get; init; }

    [JsonPropertyName("owner")]
    public bool IsOwner { get; init; }

    [JsonPropertyName("Account")]
    public AccountInfo? Account { get; init; }

    [JsonPropertyName("Server")]
    public ServerInfo? Server { get; init; }

    [JsonPropertyName("Metadata")]
    public MediaMetadata? Metadata { get; init; }

    public Uri? GenerateMediaLink()
    {
        if (Server?.Id is null || Metadata?.Key is null)
            return null;
        var escapedKey = HttpUtility.UrlEncode(Metadata.Key);
        return new Uri($"https://app.plex.tv/desktop/#!/server/{Server.Id}/details?key={escapedKey}");
    }
}

public sealed class AccountInfo
{
    public int Id { get; init; }

    [JsonPropertyName("thumb")]
    public Uri? Thumbnail { get; init; }

    public string? Title { get; init; }
}

public sealed class ServerInfo
{
    public string? Title { get; init; }

    [JsonPropertyName("uuid")]
    //[JsonConverter(typeof(UuidConverter))]
    public string? Id { get; init; }
}

public sealed class MediaMetadata
{
    public string? LibrarySectionType { get; init; }
    public string? RatingKey { get; init; }
    public string? Key { get; init; }
    public string? ParentRatingKey { get; init; }
    public string? GrandparentRatingKey { get; init; }

    [JsonPropertyName("guid")]
    public string? Id { get; init; }

    [JsonPropertyName("librarySectionID")]
    public int LibrarySectionId { get; init; }

    public string? Type { get; set; }
    public string? Title { get; init; }
    public string? GrandparentKey { get; init; }
    public string? ParentKey { get; init; }
    public string? GrandparentTitle { get; init; }
    public string? ParentTitle { get; init; }
    public string? Summary { get; init; }
    public int Index { get; init; }
    public int ParentIndex { get; init; }
    public int RatingCount { get; init; }

    [JsonPropertyName("thumb")]
    public Uri? Thumbnail { get; init; }

    public Uri? Art { get; init; }

    [JsonPropertyName("parentThumb")]
    public Uri? ParentThumbnail { get; init; }

    public Uri? ParentArt { get; init; }

    [JsonPropertyName("grandparentThumb")]
    public Uri? GrandparentThumbnail { get; init; }

    public Uri? GrandparentArt { get; init; }

    [JsonConverter(typeof(EpochTimeConverter))]
    public DateTimeOffset AddedAt { get; init; }

    [JsonConverter(typeof(EpochTimeConverter))]
    public DateTimeOffset UpdatedAt { get; init; }
}

public static class MediaTypes
{
    public const string Track = "track";
}

public class EpochTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
    }
}

public class UuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var textValue = reader.GetString();
        return Guid.TryParseExact(textValue, "N", out var parsed)
            ? parsed
            : default;
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("N"));
    }
}

public static class ApplicationExtensions
{
    private static JsonSerializerOptions jsonOptions;

    static ApplicationExtensions()
    {
        jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonOptions.TypeInfoResolverChain.Add(AppSerializationContext.Default);
    }

    public static void MapPlexWebhooks(this IEndpointRouteBuilder builder, string pattern)
    {
        builder.MapPost(pattern,
            async ([FromServices] IPlexWebhookHandler handler, [FromForm] IFormFile? thumb, [FromForm] string payload,
                CancellationToken cancellationToken) =>
            {
                Console.WriteLine(payload);
                var parsedPayload = JsonSerializer.Deserialize<PlexEventPayload>(payload, jsonOptions);
                if (parsedPayload is null)
                    return Results.Accepted();

                if (thumb is not null)
                {
                    await using var memoryStream = new MemoryStream();
                    await thumb.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Seek(default, SeekOrigin.Begin);
                    await handler.Handle(parsedPayload, memoryStream, cancellationToken);
                }
                else
                {
                    await handler.Handle(parsedPayload, cancellationToken);
                }

                return Results.Accepted();
            }).DisableAntiforgery();
    }
}

public interface IPlexWebhookHandler
{
    Task Handle(PlexEventPayload payload, Stream thumbnailStream, CancellationToken cancellationToken);
    Task Handle(PlexEventPayload payload, CancellationToken cancellationToken);
}