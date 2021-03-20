using Discord;
using Discord.WebSocket;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.Models;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot
{
    public static class SafeMessageHandler
    {
        public static async Task<int> HandleMessage(BaseMessageHandler handler, SocketUserMessage message)
        {
            try
            {
                await handler.Execute(message);
            }
            catch (NeedUserClarificationException ex)
            {
                var embed = new EmbedBuilder
                {
                    Title = $"Argument is ambiguous.  Clarification needed!",
                    Footer = new EmbedFooterBuilder { Text = "Requested by Timbly" },
                    Timestamp = DateTime.Now
                };
                embed.AddField(ex.Message, string.Join("\n", ex.ClarificationContext));
                await message.Channel.SendMessageAsync("", embed: embed.Build());
            }

            return 1;
        }
    }
}
