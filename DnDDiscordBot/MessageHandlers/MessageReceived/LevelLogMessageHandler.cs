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
                    var levelLog = await levelLogService.HandleMessage(message, true);

                    if (!levelLog.IsValid())
                    {
                        await message.AddReactionAsync(new Emoji("❌"));
                        if( levelLog.HasBannedSpell )
                        {
                            if (Emote.TryParse("<:badmagic:983063368824946759>", out var emote))
                            {
                                await message.AddReactionAsync(emote);
                            }
                        }
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
