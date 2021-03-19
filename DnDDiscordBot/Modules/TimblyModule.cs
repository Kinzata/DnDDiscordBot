using CommandLine;
using Discord;
using Discord.Commands;
using DnDDiscordBot.Commands;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var parser = new Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<PingOptions, CharactersOptions, QuestOptions>(list);

            parserResult.MapResult(
                (PingOptions opts) => ExecuteCommand(HandlePingMessage, opts, list).Result,
                (CharactersOptions opts) => ExecuteCommand(HandleCharacterMessage, opts, list).Result,
                (QuestOptions opts) => ExecuteCommand(HandleQuestMessage, opts, list).Result,
                errs => 1);

            await parserResult.HandleHelpRequestedErrorAsync(Context);

        }

        public Task<int> HandlePingMessage(object commandOptions, List<string> messageContents)
        {
            var options = (PingOptions)commandOptions;
            new PingCommand().Execute(Context, options);
            return Task.FromResult(1);
        }

        public async Task<int> HandleCharacterMessage(object commandOptions, List<string> messageContents)
        {
            var options = (CharactersOptions)commandOptions;

            messageContents.RemoveAt(0); // Remove command that got us here
            await new CharactersCommand(_services).Execute(Context, options, messageContents);
            Console.WriteLine("Characters.");
            return 1;
        }

        public async Task<int> HandleQuestMessage(object commandOptions, List<string> messageContents)
        {
            var options = (QuestOptions)commandOptions;

            messageContents.RemoveAt(0); // Remove command that got us here
            await new QuestCommand(_services).Execute(Context, options, messageContents);
            Console.WriteLine("Quest.");
            return 1;
        }

        public async Task<int> ExecuteCommand(Func<object, List<string>, Task<int>> commandFunc, object options, List<string> messageContents)
        {
            try
            {
                await commandFunc(options, messageContents);
            }
            catch (NeedUserClarificationException ex )
            {
                var embed = new EmbedBuilder
                {
                    Title = $"Clarification Needed!",
                    Footer = new EmbedFooterBuilder { Text = "Requested by Timbly" },
                    Timestamp = DateTime.Now
                };
                embed.AddField(ex.Message, string.Join("\n", ex.ClarificationContext));
                await Context.Channel.SendMessageAsync("", embed: embed.Build());
            }

            return 1;
        }
    }
}
