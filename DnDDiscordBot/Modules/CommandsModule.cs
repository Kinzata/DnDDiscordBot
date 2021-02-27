using Discord.Commands;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    [Group("timbly")]
    public class CommandsModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Summary("")]
        public Task PingAsync()
        {
            var channel = Context.Channel;

            channel.SendMessageAsync("Pong!");

            return Task.CompletedTask;
        }

    }
}
