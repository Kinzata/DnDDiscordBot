using Discord;
using Discord.WebSocket;
using DnDDiscordBot.Services;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot.MessageHandlers.MessageReceived
{
    public class LevelLogMessageHandler
    {
        public async Task ExecuteAsync(SocketUserMessage message, IServiceProvider services)
        {
            var levelLogService = (LevelLogService)services.GetService(typeof(LevelLogService));

            await Task.Run(async () =>
            {
                var wasParsed = await levelLogService.HandleMessage(message, true);

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
    }
}
