using DSharpPlus;
using DSharpPlus.SlashCommands;
using GravyPlex.CommandModules;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();
builder.AddEnvironmentVariables();
Configuration = builder.Build();
var discord = new DiscordClient(new()
{
    Token = Configuration["DISCORD_TOKEN"]!,
    TokenType = TokenType.Bot,
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
});
var slash = discord.UseSlashCommands();
slash.RegisterCommands<PlexRequestCommandModule>();

await discord.ConnectAsync();
await Task.Delay(-1);

public partial class Program
{
    public static IConfiguration Configuration = null!;
}