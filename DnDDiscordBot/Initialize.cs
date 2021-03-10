using Amazon.DynamoDBv2;
using Discord.Commands;
using Discord.WebSocket;
using DnDDiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DnDDiscordBot
{
    public class Initialize
	{
		private readonly CommandService _commands;
		private readonly DiscordSocketClient _discordClient;
		private readonly AmazonDynamoDBClient _dbClient;

		// Ask if there are existing CommandService and DiscordSocketClient
		// instance. If there are, we retrieve them and add them to the
		// DI container; if not, we create our own.
		public Initialize(CommandService commands = null, DiscordSocketClient discordClient = null, AmazonDynamoDBClient dbClient = null)
		{
			_commands = commands ?? new CommandService();
			_discordClient = discordClient ?? new DiscordSocketClient();
			_dbClient = dbClient ?? new AmazonDynamoDBClient();
		}

		public IServiceProvider BuildServiceProvider() => new ServiceCollection()
			.AddSingleton(_discordClient)
			.AddSingleton(_commands)
			.AddSingleton(_dbClient)

			.AddSingleton<DynamoService>()
			.AddSingleton<LevelLogService>()
			.AddSingleton<UserCommandStateService>()
			.AddSingleton<CommandHandler>()
			.AddSingleton<QuestService>()
			.BuildServiceProvider();
	}
}
