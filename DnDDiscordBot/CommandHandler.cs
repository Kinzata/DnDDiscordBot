using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DnDDiscordBot.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DnDDiscordBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        private const string LEVEL_LOG_CHANNEL = "level-up-log";

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;
            _client.MessageReceived += HandleMessageAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: _services);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            if (message.Channel.Name != LEVEL_LOG_CHANNEL) return;

            // Determine if the message is not a command based on the prefix and make sure no bots trigger this
            if ((message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var levelLogService = (LevelLogService) _services.GetService(typeof(LevelLogService));


            await Task.Run(async () =>
            {
                var wasParsed = levelLogService.HandleMessage(message);

                if (!wasParsed)
                {
                    await message.AddReactionAsync(new Emoji("❌"));
                }
                else
                {
                    await message.AddReactionAsync(new Emoji("✅"));
                }
            });
        }


        public async Task ReactWithEmoteAsync(SocketUserMessage userMsg, string escapedEmote)
        {
            if (Emote.TryParse(escapedEmote, out var emote))
            {
                await userMsg.AddReactionAsync(emote);
            }
        }
    }
}
