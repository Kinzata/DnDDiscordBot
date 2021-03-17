using Discord.WebSocket;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace DnDDiscordBot.MessageHandlers.MessageReceived
{
    public class DirectMessageHandler
    {
        public async Task ExecuteAsync(SocketUserMessage message, SocketDMChannel channel, IServiceProvider services)
        {
            var userCommandStateService = (UserCommandStateService)services.GetService(typeof(UserCommandStateService));
            var questService = (QuestService)services.GetService(typeof(QuestService));

            var user = message.Author;

            var state = userCommandStateService.GetCommandStateForUser(user);

            var keyCommand = Keywords.Where(w => message.Content == w).Select(w => w).FirstOrDefault();

            if( !string.IsNullOrWhiteSpace(keyCommand))
            {
                await HandleKeywords(keyCommand, message, channel, services);
                return;
            }

            if ( state == CommandState.QuestCreation)
            {
                var success = await questService.TryRegisterQuest(message);
                if( success )
                {
                    await channel.SendMessageAsync($"Quest registered!");
                    userCommandStateService.InitForUser(user);
                }
            }
            else
            {
                await channel.SendMessageAsync($"Your current state is: {state}");
            }
        }

        private static string[] Keywords = { "cancel" };

        private async Task HandleKeywords(string keyword, SocketUserMessage message, SocketDMChannel channel, IServiceProvider services){
            switch (keyword)
            {
                case "cancel":
                    await HandleCancelAsync(message, channel, services);
                    break;
            }
        }

        private async Task HandleCancelAsync(SocketUserMessage message, SocketDMChannel channel, IServiceProvider services)
        {
            var userCommandStateService = (UserCommandStateService)services.GetService(typeof(UserCommandStateService));
            userCommandStateService.InitForUser(message.Author);
            await channel.SendMessageAsync($"User state has been reset.  Sorry it didn't work out.");
        }
    }
}
