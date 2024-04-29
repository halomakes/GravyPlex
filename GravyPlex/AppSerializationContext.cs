using System.Text.Json.Serialization;
using Plex.Webhooks;

namespace GravyPlex;

    [JsonSerializable(typeof(PlexEventPayload))]
    internal partial class AppSerializationContext : JsonSerializerContext;