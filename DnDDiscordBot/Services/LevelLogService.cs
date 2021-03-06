﻿using Discord;
using DnDDiscordBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DnDDiscordBot.Services
{
    public class LevelLogService
    {
        private const string CHARACTER_DATA_STATIC_FILENAME = "DnDServerCharacterLevels.json";

        private Dictionary<string, LevelLog> _characterLevels;
        private readonly DynamoService _dynamoService;

        public LevelLogService(DynamoService dynamoService)
        {
            _dynamoService = dynamoService;
            _characterLevels = InitializeCharacterLog();
        }

        public List<LevelLog> GetSortedLevelLogs()
        {
            return _characterLevels.OrderByDescending(record => record.Value.Level).Select(record => record.Value).ToList();
        }

        public async Task<bool> HandleMessage(IMessage message, bool shouldSaveToDB = false)
        {
            var isParsed = false;

            var levelLog = ParseDiscordMessage(message);
            if (levelLog.IsValid())
            {
                Console.WriteLine(levelLog.ToStringSafe());
                isParsed = true;

                var updated = TryUpdateExistingRecord(levelLog, out var existingRecord);

                if( existingRecord == null )
                {
                    _characterLevels.Add(levelLog.Guid, levelLog);
                    if (shouldSaveToDB)
                    {
                        await _dynamoService.InsertCharacterAsync(levelLog);
                    }
                }
                else
                {
                    // We have an existing record, update the one in the DB
                    if (shouldSaveToDB && updated)
                    {
                        await _dynamoService.UpdateCharacterAsync(existingRecord);
                    }
                }

                UpdateLocalCache();
            }

            return isParsed;
        }

        /// <summary>
        /// Stores the LevelLog only if it is a new character, or updates the existing record if one already exists but is a lower level
        /// </summary>
        /// <param name="log"></param>
        /// <returns>True/False - Whether the existing record was updated</returns>
        private bool TryUpdateExistingRecord(LevelLog log, out LevelLog existingRecord)
        {
            existingRecord = _characterLevels.Where(record => record.Value.CharacterName == log.CharacterName).Select(record => record.Value).FirstOrDefault();
            if (existingRecord != null)
            {
                // Check if level is higher
                if (existingRecord.Level < log.Level)
                {
                    existingRecord.UpdateFrom(log);
                    return true;
                }
            }
            return false;
        }

        private Dictionary<string, LevelLog> InitializeCharacterLog()
        {
            if (File.Exists(CHARACTER_DATA_STATIC_FILENAME))
            {
                var json = File.ReadAllText(CHARACTER_DATA_STATIC_FILENAME);

                return JsonConvert.DeserializeObject<Dictionary<string, LevelLog>>(json);
            }
            else
            {
                return new Dictionary<string, LevelLog>();
            }
        }

        public LevelLog GetCharacterData(string characterName)
        {
            var character = _characterLevels.Select(row => row.Value).Where(log => log.SearchFieldCharacterName == characterName.ToLower()).FirstOrDefault();
            
            // Maybe put a check here if we don't find the character to check if it's in the DB.  Also... handle duplicates?
            
            return character;
        }

        public LevelLog[] GetCharacterData(ulong userId)
        {
            var characters = _characterLevels.Select(row => row.Value).Where(log => log.UserId == userId).ToArray();

            // Maybe put a check here if we don't find the character to check if it's in the DB.  Also... handle duplicates?

            return characters;
        }

        public List<LevelLog> RetrieveAllCharacterData()
        {
            // For now, we store this in memory with a database and local cache backup.
            // The data in memory is expected to be correct, so just return that

            return GetSortedLevelLogs();
        }

        public async Task DeleteCharacterDataAsync(string characterName)
        {
            var key = _characterLevels.Where(row => row.Value.SearchFieldCharacterName == characterName.ToLower()).Select(row => row.Key).FirstOrDefault();
            
            if( !string.IsNullOrEmpty(key) )
            {
                _characterLevels.Remove(key);
                UpdateLocalCache();
                await _dynamoService.DeleteCharacterRecord(key);
            }
        }

        public async Task UpdateCacheFromDBAsync()
        {
            //var data = _dynamoService.GetAllCharacterData();

        }

        /// <summary>
        /// Save character list to both DB and cache
        /// </summary>
        public async Task SaveCharacterListAsync()
        {
            UpdateLocalCache();
            await _dynamoService.InsertCharacterListAsync(GetSortedLevelLogs());
        }

        /// <summary>
        /// Updates local cache of character data only
        /// </summary>
        /// <param name="logs"></param>
        private void UpdateLocalCache()
        {
            var jsonString = JsonConvert.SerializeObject(_characterLevels);

            File.WriteAllText(CHARACTER_DATA_STATIC_FILENAME, jsonString);
        }

        public LevelLog ParseDiscordMessage(IMessage message)
        {
            var levelLog = new LevelLog();

            levelLog.Guid = Guid.NewGuid().ToString();
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
