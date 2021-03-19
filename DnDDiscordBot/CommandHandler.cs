using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DnDDiscordBot.Commands;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.MessageHandlers.MessageReceived;
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
            _client.MessageUpdated += HandleMessageUpdatedAsync;

            _commands.CommandExecuted += OnCommandExecutedAsync;

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

            // Determine if the message is not a command based on the prefix and make sure no bots trigger this
            if ((message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var dmChannel = message.Channel as SocketDMChannel;
            if( dmChannel != null)
            {
                await new DirectMessageHandler().ExecuteAsync(message, dmChannel, _services);
            }
            else
            {
                if( message.Channel.Name == LEVEL_LOG_CHANNEL )
                {
                    await new LevelLogMessageHandler().ExecuteAsync(message, _services);
                }
            }

        }

        private async Task HandleMessageUpdatedAsync(Cacheable<IMessage, ulong> cache, SocketMessage newMessageParam, ISocketMessageChannel channel)
        {
            // Don't process the command if it was a system message
            var newMessage = newMessageParam as SocketUserMessage;
            if (newMessage == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is not a command based on the prefix and make sure no bots trigger this
            if ((newMessage.HasCharPrefix('!', ref argPos) ||
                newMessage.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                newMessage.Author.IsBot)
                return;

            if (newMessage.Channel.Name == LEVEL_LOG_CHANNEL)
            {
                await newMessage.RemoveAllReactionsAsync();
                await new LevelLogMessageHandler().ExecuteAsync(newMessage, _services);
            }
        }

        public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason) && result.ErrorReason != "Unknown command.")
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}
