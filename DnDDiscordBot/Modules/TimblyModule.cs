using CommandLine;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DnDDiscordBot.Commands;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.Extensions;
using DnDDiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    [Group("timbly")]
    [Alias("tim", "registry")]
    public class TimblyModule : ModuleBase<SocketCommandContext>
    {
        private IServiceProvider _services;

        public TimblyModule(IServiceProvider services)
        {
            _services = services;
        }

        [Command]
        public async Task Execute([Remainder] string input)
        {
            var content = Context.Message.Content;
            var args = content.SplitArgs();
            var list = new List<string>(args);
            list.RemoveAt(0); // Remove command that got us here

            var actionContext = new DndActionContext
            {
                DiscordContext = Context,
                MessageContents = list
            };

            if (Context.User is SocketGuildUser gUser)
            {
                actionContext.Roles = gUser.Roles;
            }

            var parser = new Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<PingOptions, CharactersOptions, QuestOptions>(list);

            parserResult.MapResult(
                (PingOptions opts) => SafeCommandExecutor.ExecuteCommand(new PingCommand(_services), opts, actionContext).Result,
                (CharactersOptions opts) => SafeCommandExecutor.ExecuteCommand(new CharactersCommand(_services), opts, actionContext).Result,
                (QuestOptions opts) => SafeCommandExecutor.ExecuteCommand(new QuestCommand(_services), opts, actionContext).Result,
                errs => 1);

            await parserResult.HandleHelpRequestedErrorAsync(Context);

        }


    }
}
