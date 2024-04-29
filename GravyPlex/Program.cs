using DSharpPlus;
using GravyPlex;
using GravyPlex.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plex.Webhooks;
using System.Text.Json;
using GravyPlex.Services;
using Microsoft.Extensions.Options;

var appBuilder = WebApplication.CreateBuilder(args);
appBuilder.Configuration.AddUserSecrets<Program>();
appBuilder.Configuration.AddEnvironmentVariables();
AddDiscordBot(appBuilder);

appBuilder.Services.AddScoped<IPlexWebhookHandler, MediaAddedHandler>();
appBuilder.Services.ConfigureHttpJsonOptions(static opts =>
{
    opts.SerializerOptions.TypeInfoResolverChain.Add(AppSerializationContext.Default);
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
appBuilder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.CombineLogs = true;
});


var app = appBuilder.Build();
app.MapPlexWebhooks("plex-event");

app.UseStatusCodePages(async statusCodeContext
    => await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusCodeContext.HttpContext));
Configuration = app.Configuration;
app.UseHttpLogging();
app.Run();
return;

void AddDiscordBot(WebApplicationBuilder b)
{
    b.Services.Configure<DiscordConfiguration>(c =>
    {
        c.Token = b.Configuration.GetValue<string>("Discord_Token")!;
        c.TokenType = TokenType.Bot;
        c.Intents = DiscordIntents.MessageContents | DiscordIntents.Guilds | DiscordIntents.AllUnprivileged;
    });
    b.Services.AddSingleton<DiscordClient>(ctx => new(ctx.GetRequiredService<IOptions<DiscordConfiguration>>().Value));
    b.Services.AddHostedService<DiscordBotService>();
}

public partial class Program
{
    public static IConfiguration Configuration = null!;
}