﻿using DSharpPlus;
 using DSharpPlus.SlashCommands;
 using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GravyPlex.CommandModules;

namespace GravyPlex.Services;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordClient _client;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IServiceProvider _services;

    public DiscordBotService(DiscordClient client, ILogger<DiscordBotService> logger, IServiceProvider services)
    {
        _client = client;
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // just run forever
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var commands = _client.UseSlashCommands(new()
        {
            Services = _services
        });
        _client.ClientErrored += (_, x) =>
        {
            _logger.LogError(x.Exception, "Discord client error");
            return Task.CompletedTask;
        };

        commands.RegisterCommands<PlexRequestCommandModule>();
        await _client.ConnectAsync();
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.DisconnectAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _client.Dispose();
        base.Dispose();
    }
}