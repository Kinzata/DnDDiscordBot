using Discord.WebSocket;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Threading.Tasks;

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
      
            if( state == CommandState.QuestCreation)
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
    }
}
