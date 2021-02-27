using Discord;
using DnDDiscordBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DnDDiscordBot.Services
{
    public class LevelLogService
    {
        private const string CHARACTER_DATA_STATIC_FILENAME = "DnDServerCharacterLevels.json";

        private Dictionary<ulong, LevelLog> _characterLevels;

        public LevelLogService()
        {
            _characterLevels = InitializeCharacterLog();
        }

        public List<LevelLog> GetSortedLevelLogs()
        {
            return _characterLevels.OrderByDescending(record => record.Value.Level).Select(record => record.Value).ToList();
        }

        public bool HandleMessage(IMessage message)
        {
            var isParsed = false;

            var messageId = message.Id;
            if (_characterLevels.ContainsKey(messageId))
            {
                Console.WriteLine($"Duplicate message read, skipping -- {messageId}");
            }
            else
            {
                var levelLog = ParseDiscordMessage(message);
                if (levelLog.IsValid())
                {
                    InsertLevelLogIfNotDuplicate(messageId, levelLog);
                    Console.WriteLine(levelLog.ToStringSafe());
                    isParsed = true;
                }
            }

            return isParsed;
        }

        private void InsertLevelLogIfNotDuplicate(ulong messageId, LevelLog log)
        {
            var existingRecord = _characterLevels.Where(record => record.Value.CharacterName == log.CharacterName).Select(record => record.Value).FirstOrDefault();
            if (existingRecord != null)
            {
                // Check if level is higher
                if (existingRecord.Level <= log.Level)
                {
                    _characterLevels.Remove(existingRecord.MessageId);
                    _characterLevels.Add(log.MessageId, log);
                }
            }
            else
            {
                _characterLevels.Add(messageId, log);
            }
        }

        private Dictionary<ulong, LevelLog> InitializeCharacterLog()
        {
            if (File.Exists(CHARACTER_DATA_STATIC_FILENAME))
            {
                var json = File.ReadAllText(CHARACTER_DATA_STATIC_FILENAME);

                return JsonConvert.DeserializeObject<Dictionary<ulong, LevelLog>>(json);
            }
            else
            {
                return new Dictionary<ulong, LevelLog>();
            }
        }

        public void ExportCharacterLevels()
        {
            var jsonString = JsonConvert.SerializeObject(_characterLevels);

            File.WriteAllText(CHARACTER_DATA_STATIC_FILENAME, jsonString);
        }

        public LevelLog ParseDiscordMessage(IMessage message)
        {
            var levelLog = new LevelLog();

            levelLog.MessageId = message.Id;
            levelLog.UserId = ParseOwnerIdFromContent(message.Content);
            levelLog.CharacterName = ParseCharacterNameFromContent(message.Content);
            levelLog.Level = ParseLevelFromContent(message.Content);

            return levelLog;
        }

        public ulong ParseOwnerIdFromContent(string content)
        {
            ulong id = 0;

            var rx = new Regex(@"<@!?(?<ulong>\d*)>",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Matches(content);
            if (matches.Count > 0)
            {
                id = ulong.Parse(matches[0].Groups["ulong"].Value);
            }
           
            return id;
        }

        public string ParseCharacterNameFromContent(string content)
        {
            var name = "";

            content = content.Split('\n')[0];

            var rx = new Regex(@"as (?<name>.*)\s?(gains|gained|gain|got|has|pitfought|\n|\r)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Matches(content);
            if( matches.Count > 0 )
            {
                name = matches[0].Groups["name"].Value;
                name = name.Trim();
            }
           
            // Trim off final comma (lumped in with name) but we want to allow names with commas
            if( name.EndsWith(',') )
            {
                name = name.Remove(name.Length - 1);
            }


            return name;
        }

        public int ParseLevelFromContent(string content)
        {
            var level = "0";

            content = content.Split('\n')[0];

            var rx = new Regex(@"level (?<level>\d+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Matches(content);
            if (matches.Count > 0)
            {
                level = matches[0].Groups["level"].Value;
                level = level.Trim();
            }

            return int.Parse(level);
        }
    }
}
