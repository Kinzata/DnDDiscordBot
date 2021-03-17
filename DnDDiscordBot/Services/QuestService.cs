using Discord.WebSocket;
using DnDDiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DnDDiscordBot.Services
{
    public class QuestService
    {
        private readonly string QuestNameRegex = @"Quest Name: (?<value>.*)";
        private readonly string LevelRangeRegex = @"Recommended Levels: (?<value>\d+-\d+)";
        private readonly string PlayerCountRegex = @"Player Count: (?<value>\d+-\d+)";
        private readonly string QuestTypeRegex = @"Type: (?<value>.*)";
        private readonly string DescriptionRegex = @"Description: (?<value>.*)";

        private LevelLogService _levelLogService;

        public QuestService(LevelLogService levelLogService)
        {
            _levelLogService = levelLogService;
        }

        public async Task<bool> TryRegisterQuest(SocketUserMessage message)
        {
            // Message should be 4 lines
            try
            {
                var lines = message.Content.Split('\n');
                if( lines.Length < 5)
                {
                    throw new Exception("Format not correct.  Expecting at least 5 lines.");
                }

                var questName = ParseRegexContentFromString(lines[0], QuestNameRegex);
                var levelRangeString = ParseRegexContentFromString(lines[1], LevelRangeRegex);
                var playerCountString = ParseRegexContentFromString(lines[2], PlayerCountRegex);
                var questTypeString = ParseRegexContentFromString(lines[3], QuestTypeRegex);
                var descriptionString = ParseRegexContentFromString(lines[4], DescriptionRegex) + "\n";
                var owner = message.Author.Id;

                // Add the rest to description
                for( int i = 5; i < lines.Length; i++ )
                {
                    descriptionString += lines[i] + "\n";
                }

                var quest = new Quest
                {
                    Guid = Guid.NewGuid().ToString(),
                    OwnerId = owner,
                    QuestName = questName,
                    LevelRangeLow = int.Parse(levelRangeString.Split('-')[0]),
                    LevelRangeHigh = int.Parse(levelRangeString.Split('-')[1]),
                    PlayerCountLow = int.Parse(playerCountString.Split('-')[0]),
                    PlayerCountHigh = int.Parse(playerCountString.Split('-')[1]),
                    QuestType = questTypeString,
                    Description = descriptionString,
                    CharactersRegistered = new List<string>()
                };

                string questBoardPosting = $@"
@Player
Quest Name: {questName}
Recommended Levels: {quest.LevelRangeLow}-{quest.LevelRangeHigh}
Player Count: {quest.PlayerCountLow}-{quest.PlayerCountHigh}
Type: {questTypeString}
Description: {descriptionString}
";
                await message.Channel.SendMessageAsync(questBoardPosting);

                return true;
            }
            catch( Exception ex )
            {
                await message.Channel.SendMessageAsync($"There was an error processing your request: {ex.Message}");
                return false;
            }
        }

        private string ParseRegexContentFromString(string content, string regex)
        {
            var name = "";

            var rx = new Regex(regex,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Matches(content);
            if (matches.Count > 0)
            {
                name = matches[0].Groups["value"].Value;
                name = name.Trim();
            }

            return name;
        }

    }
}
