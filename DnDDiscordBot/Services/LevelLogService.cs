using Discord;
using DnDDiscordBot.Exceptions;
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
                    await SaveLevelLog(levelLog, shouldSaveToDB);
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

        public async Task SaveLevelLog(LevelLog log, bool shouldSaveToDB = false)
        {
            _characterLevels.Add(log.Guid, log);
            if (shouldSaveToDB)
            {
                await _dynamoService.InsertCharacterAsync(log);
            }
        }

        /// <summary>
        /// Stores the LevelLog only if it is a new character, or updates the existing record if one already exists but is a lower level
        /// </summary>
        /// <returns>True/False - Whether the existing record was updated</returns>
        private bool TryUpdateExistingRecord(LevelLog log, out LevelLog existingRecord)
        {
            var existingRecords = _characterLevels
                // User Ids match
                .Where(r => r.Value.UserId == log.UserId)
                // One of the names contains the other
                .Where(r => r.Value.SearchFieldCharacterName.Contains(log.SearchFieldCharacterName) || log.SearchFieldCharacterName.Contains(r.Value.SearchFieldCharacterName))
                .Select(r => r.Value)
                .ToList();

            if( existingRecords.Count() <= 1)
            {
                existingRecord = existingRecords.FirstOrDefault();
            }
            else
            {
                var clarificationContext = existingRecords.Select(l => l.CharacterName);
                throw new NeedUserClarificationException("Multiple character records exist.  Please merge them using `!timbly c merge --help`.", clarificationContext.ToList());
            }
                
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

        public IEnumerable<LevelLog> GetCharacterData(string characterName)
        {
            var characters = _characterLevels.Select(row => row.Value)
                .Where(log => log.SearchFieldCharacterName.Contains(characterName.ToLower()));
            
            // Maybe put a check here if we don't find the character to check if it's in the DB.  Also... handle duplicates?
            
            return characters;
        }

        public IEnumerable<LevelLog> FilterListByCharacterName(IEnumerable<LevelLog> list, string characterName)
        {
            list = list.Where(log => log.SearchFieldCharacterName.Contains(characterName.ToLower()));

            // Maybe put a check here if we don't find the character to check if it's in the DB.  Also... handle duplicates?

            return list;
        }

        public IEnumerable<LevelLog> FilterListByUser(IEnumerable<LevelLog> list, IGuildUser user)
        {
            if( user == null)
            {
                return new List<LevelLog>();
            }
            list = list.Where(log => log.UserId == user.Id);

            // Maybe put a check here if we don't find the character to check if it's in the DB.  Also... handle duplicates?

            return list;
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
        public void UpdateLocalCache()
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
