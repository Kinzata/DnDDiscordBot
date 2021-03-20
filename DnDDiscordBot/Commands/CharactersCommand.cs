using CommandLine;
using Discord;
using Discord.Commands;
using DnDDiscordBot.Extensions;
using DnDDiscordBot.Helpers;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Commands
{
    [Verb("characters", aliases: new[] { "c", "char" }, HelpText = "Retrieve character data.")]
    public class CharactersOptions
    {
        public static string[] Subcommands => new string[] {"delete", "merge"};

        public static string HelpHeader => "!timbly <__characters | c | char__> [args]";

        [Option('n', "name", Default = null, Required = false,
            HelpText = "-n <character name> - Search records for a character by this name.")]
        public string CharacterName { get; set; }

        [Option('u', "username", Default = null, Required = false,
            HelpText = "-u <discord username> - Search records for all characters owned by this user.")]
        public string UserName { get; set; }

        [Option('l', "levels", Default = null, Required = false,
            HelpText = "-l <low-high> - Search records for all characters within a level range. Low and high level bounds, separated by a dash or comma")]
        public string Levels { get; set; }

        [Option('v', "verbose", Default = false, Required = false,
            HelpText = "-v - Display verbose data for each character")]
        public bool Details { get; set; }

        [Subcommand]
        [Option("delete", Default = false, Required = false, 
            HelpText = "Delete a character")]
        public bool Delete { get; set; }

        [Subcommand]
        [Option("merge", Default = false, Required = false,
    HelpText = "Merge character data with others.")]
        public bool Merge { get; set; }

        [Option("help", Default = false, Required = false, Hidden = true, HelpText = "This field is needed to allow help in subverbs.")]
        public bool Help { get; set; }

        private readonly char[] delimiters = { ',', '-' };

        public int low
        {
            get
            {
                var low = 0;
                var success = false;

                foreach (var delimiter in delimiters)
                {
                    success = int.TryParse(Levels.Split(delimiter)[0], out low);
                    if (success) return low;
                }

                // Try a final parse to see if only one argument was sent in
                success = int.TryParse(Levels, out low);
                if (success) return low;

                // If nothing can be parsed, error out
                throw new Exception("Unable to parse 'level' input.");
            }
        }
        public int high
        {
            get
            {
                var high = 0;
                var success = false;

                foreach (var delimiter in delimiters)
                {
                    var split = Levels.Split(delimiter);
                    if (split.Length != 2) continue;

                    success = int.TryParse(split[1], out high);
                    if (success) return high;
                }

                // Try a final parse to see if only one argument was sent in
                success = int.TryParse(Levels, out high);
                if (success) return high;

                // If nothing can be parsed, error out
                throw new Exception("Unable to parse 'level' input.");
            }
        }
    }

    public class CharactersCommand : BaseCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LevelLogService _levelLogService;

        private const string LEVEL_LOG_CHANNEL = "level-up-log";

        public CharactersCommand(IServiceProvider services) : base(services)
        {
            _serviceProvider = services;
            _levelLogService = (LevelLogService)services.GetService(typeof(LevelLogService));
        }

        public override async Task Execute(object commandArgs, DndActionContext actionContext)
        {
            var args = (CharactersOptions)commandArgs;

            var roles = actionContext.Roles;
            var messageContents = actionContext.MessageContents;
            var discordContext = actionContext.DiscordContext;

            // Handle subcommands
            if ( messageContents.Count > 0 )
            {
                var nextArg = messageContents[0];

                if (CharactersOptions.Subcommands.Contains(nextArg))
                {
                    var parser = new Parser(with => with.HelpWriter = null);

                    var parserResult = parser.ParseArguments<CharacterDeleteOptions, MergeCharactersOptions>(messageContents);

                    parserResult.MapResult(
                      (CharacterDeleteOptions opts) => SafeCommandExecutor.ExecuteCommand(new DeleteCharacterCommand(_serviceProvider), opts, actionContext).Result,
                      (MergeCharactersOptions opts) => SafeCommandExecutor.ExecuteCommand(new MergeCharactersSubCommand(_serviceProvider), opts, actionContext).Result,
                      errs => 1);

                    await parserResult.HandleHelpRequestedErrorAsync(actionContext.DiscordContext);

                    return;
                }
            }

            IEnumerable<LevelLog> logs = _levelLogService.RetrieveAllCharacterData();

            // CharacterName
            if (!string.IsNullOrWhiteSpace(args.CharacterName))
            {
                logs = _levelLogService.FilterListByCharacterName(logs, args.CharacterName);
            }
            
            // Username
            if (!string.IsNullOrWhiteSpace(args.UserName)) {
                var user = await discordContext.Guild.GetUsersAsync().Flatten().Where(u => u.Username == args.UserName).FirstOrDefaultAsync();
                logs = _levelLogService.FilterListByUser(logs, user);
            }
            
            // Levels
            if (args.Levels != null)
            {
                logs = logs.Where(log =>
                       log.Level >= args.low
                    && log.Level <= args.high);
            }
            
            await SendCharacterDataToChannel(discordContext, logs.ToList(), args.Details);

            return;
        }

        //[Command("updateCache")]
        //[RequireRole("Moderator")]
        //[Summary("Force local cache to update from DB.")]
        //public async Task ForceUpdateCache()
        //{
        //    var messages = DiscordContextHelpers.RetrieveMessagesFromChannel(Context, LEVEL_LOG_CHANNEL, 1);

        //    if (messages == null)
        //    {
        //        return;
        //    }

        //    foreach (var message in messages)
        //    {
        //        await _levelLogService.HandleMessage(message, false);
        //    }

        //    await _levelLogService.SaveCharacterListAsync();
        //}

        // TODO - This isn't hooked up to the command yet
        [Command("recordLevelLogDb")]
        [Summary("Read level log messages and write parsed data to the database.")]
        public async Task ReadLogAsyncDb(SocketCommandContext context, int numberOfLogsToRead)
        {
            // Check Role
            var hasPermissions = DiscordContextHelpers.UserHasRole(context.User, "Moderator");

            if (hasPermissions)
            {
                var messages = DiscordContextHelpers.RetrieveMessagesFromChannel(context, LEVEL_LOG_CHANNEL, numberOfLogsToRead);

                if (messages == null)
                {
                    return;
                }

                foreach (var message in messages)
                {
                    await _levelLogService.HandleMessage(message, false);
                }

                await _levelLogService.SaveCharacterListAsync();
            }
            else
            {
                await context.Message.Channel.SendMessageAsync($"You must have the role \"Moderator\" to run this command.");
            }
        }

        private async Task SendCharacterDataToChannel(SocketCommandContext context, List<LevelLog> logs, bool? detailedData = false)
        {
            if( detailedData.HasValue && detailedData.Value )
            {
                await SendFormattedDetailedCharacterDataToChannel(context, logs);
            }
            else
            {
                await SendSummarizedCharacterDataToChannel(context, logs);
            }
        }

        private async Task SendFormattedDetailedCharacterDataToChannel(SocketCommandContext context, List<LevelLog> logs)
        {
            var channel = context.Channel;

            if(logs != null && logs.Count() > 0)
            {
                var page = 1;
                var embed = new EmbedBuilder
                {
                    Title = $"Timbly's Character Records: Page {page++}",
                    Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                    Timestamp = DateTime.Now
                };

                var fieldCount = 0;
                var groups = logs.GroupBy(row => row.UserId).ToList();
                try
                {
                    foreach (var group in groups)
                    {
                        var characterData = "";
                        var logList = group.OrderBy(log => log.CharacterName).ToList();
                        foreach (var character in group.OrderBy(log => log.CharacterName))
                        {
                            characterData += character.ToStringSafe() + "\n";
                        }
                        var user = context.Client.GetUser(group.Key);

                        embed.AddField(user?.Username ?? "Unknown User", characterData);
                        fieldCount++;

                        if( fieldCount == 25 )
                        {
                            await channel.SendMessageAsync(embed: embed.Build());
                            embed = new EmbedBuilder
                            {
                                Title = $"Timbly's Character Records: Page {page}",
                                Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                                Timestamp = DateTime.Now
                            };
                            fieldCount = 0;
                        }
                    }
                }
                catch(Exception ex)
                {
                    await channel.SendMessageAsync("There was an issue retrieving character data...:" + ex.Message);
                }
               
                if( fieldCount != 0)
                {
                    await channel.SendMessageAsync(embed: embed.Build());
                }
            }
            else
            {
                await channel.SendMessageAsync("No characters with the specified conditions were found.");
            }
        }

        private async Task SendSummarizedCharacterDataToChannel(SocketCommandContext context, List<LevelLog> logs)
        {
            var channel = context.Channel;

            if (logs != null && logs.Count() > 0)
            {
                var embed = new EmbedBuilder
                {
                    Title = $"Timbly's Character Records: Summary",
                    Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                    Timestamp = DateTime.Now
                };


                var levelGroups = logs.GroupBy(row => row.Level).OrderBy(group => group.Key).ToList();
                try
                {
                    var characterData = "";
                    foreach (var levelGroup in levelGroups)
                    {
                        characterData += $"Level {levelGroup.Key}: {levelGroup.Count()}\n";
                    }
                    embed.AddField("Character Count By Level:", characterData);
                }
                catch (Exception ex)
                {
                    await channel.SendMessageAsync("There was an issue retrieving character data...:" + ex.Message);
                }

                await channel.SendMessageAsync(embed: embed.Build());
            }
            else
            {
                await channel.SendMessageAsync("No characters with the specified conditions were found.");
            }
        }

        private async Task GetCharacterByName(SocketCommandContext context, string characterName)
        {
            var channel = context.Channel;

            var result = _levelLogService.GetCharacterData(characterName).FirstOrDefault();

            if( result != null)
            {
                var embed = new EmbedBuilder
                {
                    Title = result.CharacterName,
                    Description = $"Information for {result.CharacterName}",
                    Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                    Timestamp = DateTime.Now
                };
                embed.AddField("Level", result.Level);
                embed.AddField("UserId", context.Client.GetUser(result.UserId).Username);

                await channel.SendMessageAsync("", embed: embed.Build());
            }
            else
            {
                await channel.SendMessageAsync($"No character named \"{characterName}\" found.");
            }
        }

        private async Task GetCharacterByUser(SocketCommandContext context, IUser user)
        {
            var channel = context.Channel;

            var result = _levelLogService.GetCharacterData(user.Id);

            if (result != null && result.Length != 0)
            {
                var embed = new EmbedBuilder
                {
                    Title = $"{user.Username}'s Characters",
                    Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                    Timestamp = DateTime.Now
                };
                foreach (var character in result.OrderBy(c => c.CharacterName))
                {
                    embed.AddField(character.CharacterName, $"Level: {character.Level}");
                }

                await channel.SendMessageAsync("", embed: embed.Build());
            }
            else
            {
                await channel.SendMessageAsync($"No characters found for \"{user.Username}\".");
            }
        }
    }
}
