using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;

namespace DnDDiscordBot.Models
{
    public class DndActionContext
    {
        public SocketCommandContext DiscordContext { get; set; }
        public List<string> MessageContents { get; set; }
        public IReadOnlyCollection<SocketRole> Roles { get; set; }
    }
}
