using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot.Models
{
    public abstract class BaseMessageHandler
    {
        public BaseMessageHandler(IServiceProvider services)
        {

        }

        public abstract Task Execute(SocketUserMessage message);
    }
}
