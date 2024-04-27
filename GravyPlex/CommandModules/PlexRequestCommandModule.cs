using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Manatee.Trello;

namespace GravyPlex.CommandModules;

public class PlexRequestCommandModule : ApplicationCommandModule
{
    [SlashCommand("request", "Request an item be added to Plex")]
    public async Task AddRequest(InteractionContext ctx,
        [Option("type", "The type of media to request")] MediaType mediaType,
        [Option("name", "The name of the media you are requesting")] string name,
        [Option("release-year", "The year the piece of media was released")] long? releaseYear = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        try
        {
            TrelloAuthorization.Default.AppKey = Program.Configuration["TRELLO_KEY"];
            TrelloAuthorization.Default.UserToken = Program.Configuration["TRELLO_TOKEN"];
            var factory = new TrelloFactory();
            var board = factory.Board(Program.Configuration["TRELLO_BOARD"]);
            await board.Refresh();
            var list = board.Lists.First();
            var labelName = GetLabelName(mediaType);
            var label = board.Labels.First(l => l.Name == labelName);
            var card = await list.Cards.Add(
                name: releaseYear.HasValue ? $"{name} ({releaseYear})" : name,
                description: $"Automatically added via Discord integration by user {ctx.User.GlobalName}",
                labels: [label]
            );
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Card created for {labelName} {name}.")
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithUrl(card.Url)
                    .WithTitle($"{labelName} {name} on board {board.Name}")
                    .WithDescription("Track this item on the requests board at Trello")
                ));
        }
        catch (Exception e)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Uh-oh.  Something went wrong.")
                .AsEphemeral());
            Console.WriteLine(e);
        }
    }

    private static string GetLabelName(MediaType mediaType) => mediaType switch
    {
        MediaType.Movie => "Movie",
        MediaType.Music => "Music",
        MediaType.Anime => "Anime",
        MediaType.TvShow => "TV Show",
        MediaType.WebShow => "Web Show",
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, null)
    };

    public enum MediaType
    {
        Movie,
        Music,
        Anime,
        TvShow,
        WebShow
    }
}