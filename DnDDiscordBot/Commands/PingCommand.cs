using CommandLine;
using DnDDiscordBot.Models;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot.Commands
{
    [Verb("ping", HelpText = "Simple ping command to check if Timbly is in.")]
    public class PingOptions
    {
        public static string HelpHeader => "!timbly ping";
    }

    public class PingCommand : BaseCommand
    {
        public PingCommand(IServiceProvider services) : base(services)
        {
        }

        public override Task Execute(object commandArgs, DndActionContext actionContext)
        {
            var channel = actionContext.DiscordContext.Channel;

            channel.SendMessageAsync("Pong!");

            return Task.CompletedTask;
        }
    }
}
