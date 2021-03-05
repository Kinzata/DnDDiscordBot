using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DnDDiscordBot.Helpers;
using DnDDiscordBot.Models;
using DnDDiscordBot.PreConditions;
using DnDDiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    [Group("timbly")]
    [Alias("tim", "registry")]
    public class LevelLogModule : ModuleBase<SocketCommandContext>
    {
        private readonly LevelLogService _levelLogService;

        private const string LEVEL_LOG_CHANNEL = "level-up-log";

        public LevelLogModule(LevelLogService levelLogService)
        {
            _levelLogService = levelLogService;
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

        [Command("recordLevelLogDb")]
        [RequireRole("Moderator")]
        [Summary("Read level log messages and write parsed data to the database.")]
        public async Task ReadLogAsyncDb(int numberOfLogsToRead)
        {
            var messages = DiscordContextHelpers.RetrieveMessagesFromChannel(Context, LEVEL_LOG_CHANNEL, numberOfLogsToRead);

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

        [Command("characters")]
        [Alias("c", "char", "chars", "character")]
        [Summary("Retrieve character data.\n" +
            "__Valid Arguments__\n" +
            "`name: <character name>` - Character name to search for specifically\n" +
            "`levels: <low-high>` - Low and high level bounds, separated by a dash\n" +
            "`details: <true/false>` - Character data will be more detailed and individual\n")]
        public async Task GetCharacterData(LevelLogCharacterNamedArguments args = null)
        {
            // Set up roles for future use
            IReadOnlyCollection<SocketRole> roles;

            if (Context.User is SocketGuildUser gUser)
            {
                roles = gUser.Roles;
            }

            if ( !string.IsNullOrWhiteSpace(args?.name))
            {
                await GetCharacterByName(args?.name);
                return;
            }
            else if (args?.levels != null)
            {
                var logs = _levelLogService.RetrieveAllCharacterData().Where(log => 
                       log.Level >= args.low
                    && log.Level <= args.high)
                    .ToList();
                await SendCharacterDataToChannel(logs, args?.details);
            }
            else
            {
                var logs = _levelLogService.RetrieveAllCharacterData();
                await SendCharacterDataToChannel(logs, args?.details);
            }


        }

        private async Task SendCharacterDataToChannel(List<LevelLog> logs, bool? detailedData = false)
        {
            if( detailedData.HasValue && detailedData.Value )
            {
                await SendFormattedDetailedCharacterDataToChannel(logs);
            }
            else
            {
                await SendSummarizedCharacterDataToChannel(logs);
            }
        }

        private async Task SendFormattedDetailedCharacterDataToChannel(List<LevelLog> logs)
        {
            var channel = Context.Channel;

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
                        var user = Context.Client.GetUser(group.Key);

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

        private async Task SendSummarizedCharacterDataToChannel(List<LevelLog> logs)
        {
            var channel = Context.Channel;

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

        private async Task GetCharacterByName(string characterName)
        {
            var channel = Context.Channel;

            var result = _levelLogService.GetCharacterData(characterName);

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
                embed.AddField("UserId", Context.Client.GetUser(result.UserId).Username);

                await channel.SendMessageAsync("", embed: embed.Build());
            }
            else
            {
                await channel.SendMessageAsync($"No character named \"{characterName}\" found.");
            }
        }

        [Command("characters")]
        [Alias("c", "char", "chars", "character")]
        [Summary("Retrieve a specific user's character data and display as an embed.")]
        public async Task GetCharacterByUser(IUser user, LevelLogCharacterNamedArguments args = null)
        {
            var channel = Context.Channel;

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

        [Command("delete")]
        [RequireRole("Moderator")]
        [Summary("Remove a specific character's data.")]
        public async Task DeleteCharacterByName(string characterName)
        {
            await _levelLogService.DeleteCharacterDataAsync(characterName);

            await Context.Message.Channel.SendMessageAsync("Done!  If that character existed... they don't anymore!");
            
        }
    }
}
