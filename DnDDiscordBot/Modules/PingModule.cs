using Discord.Commands;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    public class PingModule : ModuleBase<SocketCommandContext>
    {
        [Command("kinzPing")]
        [Summary("")]
        public Task PingAsync()
        {
            var channel = Context.Channel;

            channel.SendMessageAsync("Pong!");

            return Task.CompletedTask;
        }

    }
}
