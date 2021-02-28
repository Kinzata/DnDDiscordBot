using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;

namespace DnDDiscordBot.Models
{
    public class LevelLog
    {
        public string Guid { get; set; }
        public ulong UserId { get; set; }
        public string CharacterName { get; set; }
        public int Level { get; set; }
        public string SearchFieldCharacterName => CharacterName.ToLower();

        public bool IsValid()
        {
            return UserId != 0 && CharacterName != "" && Level != 0;
        }
        public override string ToString()
        {
            return UserId.ToString() + "\n"
                + CharacterName + "\n"
                + Level.ToString();
        }

        public string ToStringSafe()
        {
                return CharacterName + ": Level " + Level.ToString();
        }

        public void UpdateFrom(LevelLog newRecord)
        {
            Level = newRecord.Level;
        }

        public Dictionary<string, AttributeValue> ToTransactItemDictionary()
        {
            return new Dictionary<string, AttributeValue>
            {
                {"Guid", new AttributeValue(Guid) },
                {"CharacterName", new AttributeValue(CharacterName) },
                {"SearchFieldCharacterName", new AttributeValue(SearchFieldCharacterName) },
                {"UserId", new AttributeValue(UserId.ToString()) },
                {"Level", new AttributeValue(Level.ToString()) },
            };
            
        }
    }
}
