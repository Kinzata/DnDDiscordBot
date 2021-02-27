using Discord;
using Discord.Commands;
using DnDDiscordBot.Helpers;
using DnDDiscordBot.PreConditions;
using DnDDiscordBot.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    [Group("timbly")]
    [Alias("tim", "registry")]
    public class LevelLogModule : ModuleBase<SocketCommandContext>
    {
        private readonly LevelLogService _levelLogService;
        private readonly DynamoService _dynamoService;

        private const string LEVEL_LOG_CHANNEL = "level-up-log";

        public LevelLogModule(LevelLogService levelLogService, DynamoService dynamoService)
        {
            _levelLogService = levelLogService;
            _dynamoService = dynamoService;
        }

        [Command("recordLevelLog")]
        [Summary("Read level log messages and write parsed data to a local file.")]
        public Task ReadLogAsync()
        {
            var messages = DiscordContextHelpers.RetrieveMessagesFromChannel(Context, LEVEL_LOG_CHANNEL, 300);
            
            if( messages == null) {
                return Task.CompletedTask;
            }

            foreach( var message in messages)
            {
                _levelLogService.HandleMessage(message);
            }

            _levelLogService.ExportCharacterLevels();

            return Task.CompletedTask;
        }

        [Command("recordLevelLogDb")]
        [Summary("Read level log messages and write parsed data to the database.")]
        public Task ReadLogAsyncDb()
        {
            var messages = DiscordContextHelpers.RetrieveMessagesFromChannel(Context, LEVEL_LOG_CHANNEL, 300);

            if (messages == null)
            {
                return Task.CompletedTask;
            }

            foreach (var message in messages)
            {
                _levelLogService.HandleMessage(message);
            }

            _dynamoService.InsertCharacterList(_levelLogService.GetSortedLevelLogs());

            // Export the DB list so that next time on boot we restore the same cache without needing to pull data
            _levelLogService.ExportCharacterLevels();

            return Task.CompletedTask;
        }

        [Command("characters")]
        [Alias("c", "char", "chars", "character")]
        [RequireRole("DM")]
        [Summary("Retrieve all character data and display as an embed.")]
        public async Task ServerCharactersDb()
        {
            var channel = Context.Channel;

            var result = await _dynamoService.GetAllCharacterData();

            if( result != null && result.Count() > 0)
            {
                var page = 1;
                var embed = new EmbedBuilder
                {
                    Title = $"Timbly's Character Records: Page {page++}",
                    Footer = new EmbedFooterBuilder { Text = "Recorded by Timbly" },
                    Timestamp = DateTime.Now
                };

                var fieldCount = 0;
                var groups = result.GroupBy(row => row.UserId).ToList();
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
                await channel.SendMessageAsync("There was an issue retrieving character data...");
            }

        }

        [Command("characters")]
        [Alias("c", "char", "chars", "character")]
        [Summary("Retrieve a specific character's data and display as an embed")]
        public Task GetCharacterByName(string characterName)
        {
            var channel = Context.Channel;

            var result = _dynamoService.GetCharacterData(characterName).Result;

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

                channel.SendMessageAsync("", embed: embed.Build());
            }
            else
            {
                channel.SendMessageAsync($"No character named \"{characterName}\" found.");
            }

            return Task.CompletedTask;
        }

        [Command("characters")]
        [Alias("c", "char", "chars", "character")]
        [Summary("Retrieve a specific user's character data and display as an embed.")]
        public Task GetCharacterByUser(IUser user)
        {
            var channel = Context.Channel;

            var result = _dynamoService.GetCharacterData(user.Id).Result;

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

                channel.SendMessageAsync("", embed: embed.Build());
            }
            else
            {
                channel.SendMessageAsync($"No characters found for \"{user.Username}\".");
            }

            return Task.CompletedTask;
        }

        [Command("delete")]
        [RequireRole("Moderator")]
        [Summary("Remove a specific character's data.")]
        public async Task DeleteCharacterByName(string characterName)
        {
            await _dynamoService.DeleteCharacterRecord(characterName);

            await Context.Message.Channel.SendMessageAsync("Done!  If that character existed... they don't anymore!");
            
        }
    }
}
