using Discord;
using Discord.WebSocket;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot.MessageHandlers.MessageReceived
{
    public class LevelLogMessageHandler : BaseMessageHandler
    {
        private IServiceProvider _services;

        public LevelLogMessageHandler(IServiceProvider services) : base(services)
        {
            _services = services;
        }

        public override async Task Execute(SocketUserMessage message)
        {
            var levelLogService = (LevelLogService)_services.GetService(typeof(LevelLogService));

            await Task.Run(async () =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    await message.AddReactionAsync(new Emoji("❌"));
                    throw ex;
                }
            });
        }
    }
}
