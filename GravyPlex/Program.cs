using System.Text.Json;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using GravyPlex;
using GravyPlex.CommandModules;
using GravyPlex.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plex.Webhooks;

var appBuilder = WebApplication.CreateBuilder(args);
appBuilder.Configuration.AddUserSecrets<Program>();
appBuilder.Configuration.AddEnvironmentVariables();

var discord = new DiscordClient(new()
{
    Token = appBuilder.Configuration["Discord:Token"]!,
    TokenType = TokenType.Bot,
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
});
var slash = discord.UseSlashCommands();
slash.RegisterCommands<PlexRequestCommandModule>();
await discord.ConnectAsync();

appBuilder.Services.AddScoped<DiscordClient>(_ => discord);
appBuilder.Services.AddScoped<IPlexWebhookHandler, MediaAddedHandler>();
appBuilder.Services.ConfigureHttpJsonOptions(static opts =>
{
    opts.SerializerOptions.TypeInfoResolverChain.Add(AppSerializationContext.Default);
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = appBuilder.Build();
app.MapPlexWebhooks("plex-event");

app.UseStatusCodePages(async statusCodeContext 
    => await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusCodeContext.HttpContext));
app.Run();

public partial class Program
{
    public static IConfiguration Configuration = null!;
}