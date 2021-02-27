using Discord.Commands;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    [Group("timbly")]
    [Alias("tim", "registry")]
    public class PingModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Simple ping to determine if bot is live.")]
        public Task PingAsync()
        {
            var channel = Context.Channel;

            channel.SendMessageAsync("Pong!");

            return Task.CompletedTask;
        }

    }
}
