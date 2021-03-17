using System;
using System.Collections.Generic;
using System.Text;

namespace DnDDiscordBot.Models
{
    public class Quest
    {
        public string Guid { get; set; }
        public ulong OwnerId { get; set; }
        public string QuestName { get; set; }
        public int LevelRangeLow { get; set; }
        public int LevelRangeHigh { get; set; }
        public int PlayerCountLow { get; set; }
        public int PlayerCountHigh { get; set; }
        public string QuestType { get; set; }
        public string Description { get; set; }
        public List<string> CharactersRegistered { get; set; }
    }
}
