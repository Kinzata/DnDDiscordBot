using Discord;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.Models;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot
{
    public static class SafeCommandExecutor
    {
        public static async Task<int> ExecuteCommand(BaseCommand command, object options, DndActionContext actionContext)
        {
            try
            {
                actionContext.MessageContents.RemoveAt(0); // Remove command that got us here
                await command.Execute(options, actionContext);
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
                await actionContext.DiscordContext.Channel.SendMessageAsync("", embed: embed.Build());
            }

            return 1;
        }
    }
}
