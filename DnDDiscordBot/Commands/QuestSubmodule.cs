using CommandLine;
using Discord;
using Discord.Commands;
using DnDDiscordBot.Extensions;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Commands
{
    [Verb("quest", aliases: new[] { "q" }, HelpText = "Quest related commands.")]
    public class QuestOptions
    {
        public static string[] Subcommands => new string[] { "create" };

        public static string HelpHeader => "!timbly <__quest | q__> [args]";

        [Subcommand]
        [Option("create", Default = false, Required = false,
            HelpText = "Begins the workflow for creating a quest.")]
        public bool Create { get; set; }

        [Option("help", Default = false, Required = false, Hidden = true, HelpText = "This field is needed to allow help in subverbs.")]
        public bool Help { get; set; }
    }

    [Verb("create", HelpText = "Begin the workflow for creating a quest.")]
    public class QuestCreateOptions
    {
        public static string HelpHeader => "q [ create ]";

        [Option("help", Default = false, Required = false, Hidden = true, HelpText = "This field is needed to allow help in subverbs.")]
        public bool Help { get; set; }
    }

    public class QuestCommand
    {
        private UserCommandStateService _userCommandStateService;

        public QuestCommand(IServiceProvider services)
        {
            _userCommandStateService = (UserCommandStateService)services.GetService(typeof(UserCommandStateService));
        }

        public async Task Execute(SocketCommandContext context, QuestOptions args, List<string> messageContents)
        {
            // Handle subcommands
            if (messageContents.Count > 0)
            {
                var nextArg = messageContents[0];

                if (QuestOptions.Subcommands.Contains(nextArg))
                {
                    var parser = new Parser(with => with.HelpWriter = null);

                    var parserResult = parser.ParseArguments<QuestCreateOptions, PingCommand>(messageContents);

                    parserResult.MapResult(
                      (QuestCreateOptions opts) => HandleCreateQuestMessage(context, opts).Result,
                      errs => 1);

                    await parserResult.HandleHelpRequestedErrorAsync(context);

                    return;
                }
            }
        }

        public async Task<int> HandleCreateQuestMessage(SocketCommandContext context, QuestCreateOptions args)
        {
            var commandState = _userCommandStateService.GetCommandStateForUser(context.User.Id);

            if (commandState != CommandState.None)
            {
                // begin process for resetting state

                // TEMP - just reset
                _userCommandStateService.BeginQuest(context.User.Id);
            }

            _userCommandStateService.BeginQuest(context.User.Id);

            await context.Message.AddReactionAsync(new Emoji("✅"));

            var embed = new EmbedBuilder
            {
                Title = $"Timbly's Quest Registration",
                Footer = new EmbedFooterBuilder { Text = "The law offices of Timbly, Timbly, Timbly and Associates, is not to be held responsible for any accidental or intentional death, dismemberment, or untimely banishment from the prime material plane that may happen while on an adventure. This is in accordance with Title Section \"Fables Player Contracts\",  subsection(B) paragraphs 1 thru 7." },
                Description = "Please use the template below, filling out the entire form:",
                Timestamp = DateTime.Now
            };

            string template = @"
```
Quest Name:
Recommended Levels: #-#
Player Count: #-#
Type:
Description:
```
";

            embed.AddField("Please use the template below:", template);

            embed.AddField("Extra Commands:", "cancel");

            await context.User.SendMessageAsync(embed: embed.Build());

            return 1;
        }
    }
}
