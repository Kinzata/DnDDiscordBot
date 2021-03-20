using CommandLine;
using DnDDiscordBot.Exceptions;
using DnDDiscordBot.Helpers;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Commands
{
    [Verb("delete", HelpText = "Retrieve character data.")]
    public class CharacterDeleteOptions
    {
        public static string HelpHeader => "c [ delete ]";

        [Option('n', "name", Default = null, Required = true,
            HelpText = "-n <character name> - Character to delete.")]
        public string CharacterName { get; set; }
    }

    public class DeleteCharacterCommand : BaseCommand
    {
        private readonly LevelLogService _levelLogService;

        public DeleteCharacterCommand(IServiceProvider services) : base(services)
        {
            _levelLogService = (LevelLogService)services.GetService(typeof(LevelLogService));
        }

        public override async Task Execute(object commandArgs, DndActionContext actionContext)
        {
            var args = (CharacterDeleteOptions)commandArgs;
            var discordContext = actionContext.DiscordContext;

            // Check Role
            var hasPermissions = DiscordContextHelpers.UserHasRole(discordContext.User, "Moderator");

            if (hasPermissions)
            {
                var logs = _levelLogService.GetCharacterData(args.CharacterName);
                if (logs.Count() > 1)
                {
                    var clarificationContext = logs.Select(l => l.CharacterName);
                    throw new NeedUserClarificationException("Multiple characters found.", clarificationContext.ToList());
                }

                await _levelLogService.DeleteCharacterDataAsync(args.CharacterName);

                await discordContext.Message.Channel.SendMessageAsync("Done!  If that character existed... they don't anymore!");
            }
            else
            {
                await discordContext.Message.Channel.SendMessageAsync($"You must have the role \"Moderator\" to run this command.");
            }

            return;
        }
    }


}
