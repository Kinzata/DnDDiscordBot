using CommandLine;
using Discord;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.Helpers;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Commands
{
    [Verb("merge", HelpText = "Merge character data with others.")]
    public class MergeCharactersOptions
    {
        public static string HelpHeader => "c [ merge ]";

        [Option('n', "name", Default = null, Required = true, 
            HelpText = "-n <character1> <character2> [character3...] - Characters to merge.")]
        public IEnumerable<string> CharacterName { get; set; }

        [Option('p', "pick", Default = null, Required = true,
            HelpText = "-p <existing name to use> - The name from the provided list that you want as the final record.  Must be one from the list.")]
        public string FinalChoiceCharacterName { get; set; }

        [Option('u', "username", Default = null, Required = false,
            HelpText = "-u <discord username> - User qualifier; Moderator only")]
        public string UserName { get; set; }
    }

    // Subcommand of CharactersCommand
    public class MergeCharactersSubCommand : BaseCommand
    {
        private readonly LevelLogService _levelLogService;

        public MergeCharactersSubCommand(IServiceProvider services) : base(services)
        {
            _levelLogService = (LevelLogService)services.GetService(typeof(LevelLogService));
        }

        public override async Task Execute(object commandArgs, DndActionContext actionContext)
        {
            var args = (MergeCharactersOptions)commandArgs;
            var discordContext = actionContext.DiscordContext;
            var characterNames = args.CharacterName;
            var pickedName = args.FinalChoiceCharacterName;

            var userId = discordContext.User.Id;

            if ( !string.IsNullOrWhiteSpace(args.UserName))
            {
                // Check roll for Moderator
                var hasPermissions = DiscordContextHelpers.UserHasRole(discordContext.User, "Moderator");
                if( !hasPermissions)
                {
                    await discordContext.Message.Channel.SendMessageAsync($"You must have the role \"Moderator\" to run this command with User specified.");
                    return;
                }

                var user = await DiscordContextHelpers.GetUser(discordContext, args.UserName);
                if( user == null)
                {
                    throw new NeedUserClarificationException("User not found.", new List<string> { args.UserName });
                }

                userId = user.Id;
            }

            var levelLogs = _levelLogService.GetCharacterData(userId);

            var charactersToMerge = new List<LevelLog>();

            foreach(var name in characterNames)
            {
                var character = levelLogs.Select(l => l).Where(l => l.SearchFieldCharacterName == name.ToLower()).FirstOrDefault();
                if( character == null )
                {
                    throw new NeedUserClarificationException("Character not found.", new List<string> { name });
                }
                charactersToMerge.Add(character);
            }

            var mergedLevelLog = new LevelLog();

            // Get the longest name
            var log = charactersToMerge.Where(l => l.CharacterName == pickedName).FirstOrDefault();
            if (log == null)
            {
                throw new NeedUserClarificationException("Character name you wish to use was not matched to a selected record.  Please check that you didn't mistype it.", new List<string> { pickedName });
            }
            mergedLevelLog.CharacterName = log.CharacterName;

            // Get the highest level
            log = charactersToMerge.OrderByDescending(l => l.Level).First();
            mergedLevelLog.Level = log.Level;

            mergedLevelLog.UserId = userId;
            mergedLevelLog.Guid = Guid.NewGuid().ToString();

            // Delete the other ones
            foreach(var character in charactersToMerge)
            {
                await _levelLogService.DeleteCharacterDataAsync(character.CharacterName);
            }

            await _levelLogService.SaveLevelLog(mergedLevelLog, true);

            _levelLogService.UpdateLocalCache();

            var embed = new EmbedBuilder
            {
                Title = $"Characters merged!",
                Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                Timestamp = DateTime.Now
            };
            embed.AddField("All provided names have been removed with a new record recorded for:", pickedName);
            await actionContext.DiscordContext.Channel.SendMessageAsync("", embed: embed.Build());

            return;
        }
    }


}
