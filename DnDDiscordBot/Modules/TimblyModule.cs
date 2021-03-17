using CommandLine;
using Discord.Commands;
using DnDDiscordBot.Commands;
using DnDDiscordBot.Extensions;
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
            var args = content.Split(' ');
            var list = new List<string>(args);
            list.RemoveAt(0); // Remove command that got us here

            var parser = new Parser(with => with.HelpWriter = null);

            var parserResult = parser.ParseArguments<PingOptions, CharactersOptions, QuestOptions>(list);
            
            parserResult.MapResult(
              (PingOptions opts) => HandlePingMessage(opts),
              (CharactersOptions opts) => HandleCharacterMessage(opts, list).Result,
              (QuestOptions opts) => HandleQuestMessage(opts, list).Result,
              errs => 1);

            await parserResult.HandleHelpRequestedErrorAsync(Context);
           
        }

        public int HandlePingMessage(PingOptions options)
        {
            new PingCommand().Execute(Context, options);
            return 1;
        }

        public async Task<int> HandleCharacterMessage(CharactersOptions options, List<string> messageContents)
        {
            messageContents.RemoveAt(0); // Remove command that got us here
            await new CharactersCommand(_services).Execute(Context, options, messageContents);
            Console.WriteLine("Characters.");
            return 1;
        }

        public async Task<int> HandleQuestMessage(QuestOptions options, List<string> messageContents)
        {
            messageContents.RemoveAt(0); // Remove command that got us here
            await new QuestCommand(_services).Execute(Context, options, messageContents);
            Console.WriteLine("Quest.");
            return 1;
        }

    }
}
