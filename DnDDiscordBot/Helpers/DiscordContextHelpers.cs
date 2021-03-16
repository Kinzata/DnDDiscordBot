using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace DnDDiscordBot.Helpers
{
    public static class DiscordContextHelpers
    {
        public static IEnumerable<IMessage> RetrieveMessagesFromChannel(SocketCommandContext context, string channelName, int numberOfMessages)
        {
            var socketChannel = context.Guild.Channels.Select(channel => channel).Where(channel => channel.Name.Equals(channelName)).FirstOrDefault();

            if (socketChannel == null) { return null; }

            var channel = context.Client.GetChannel(socketChannel.Id) as ISocketMessageChannel;

            var messages = channel.GetMessagesAsync(numberOfMessages, CacheMode.AllowDownload);

            var flattened = messages.FlattenAsync().Result;

            return flattened;
        }

        public static bool UserHasRole(IUser user, string roleName)
        {
            if (user is SocketGuildUser gUser)
            {
                if (gUser.Roles.Any(r => r.Name == roleName))
                    return true;
            }

            return false;
        }
    }
}
