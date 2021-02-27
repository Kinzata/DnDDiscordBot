using Amazon.DynamoDBv2;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot
{
    class Program
    {
		public static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		private DiscordSocketClient _client;

		public async Task MainAsync()
		{
			var config = new DiscordSocketConfig
			{
				AlwaysDownloadUsers = true,
				MessageCacheSize = 100
			};
			_client = new DiscordSocketClient(config);

			_client.Log += Log;
			var commands = new CommandService();

			var dbClient = new AmazonDynamoDBClient();

			var serviceProvider = new Initialize(commands, _client, dbClient).BuildServiceProvider();

			var handler = new CommandHandler(serviceProvider, _client, commands);
			await handler.InstallCommandsAsync();

			var token = Environment.GetEnvironmentVariable("DISCORD_DND_BOT_TOKEN");

			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();


			await Task.Delay(-1);
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}
}
