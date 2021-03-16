using CommandLine;
using Discord.Commands;
using System.Threading.Tasks;

namespace DnDDiscordBot.Commands
{
    [Verb("ping", HelpText = "Simple ping command to check if Timbly is in.")]
    public class PingOptions
    {
        public static string HelpHeader => "!timbly ping";
    }

    public class PingCommand
    {
        public Task Execute(SocketCommandContext context, PingOptions args)
        {
            var channel = context.Channel;

            channel.SendMessageAsync("Pong!");

            return Task.CompletedTask;
        }
    }
}
