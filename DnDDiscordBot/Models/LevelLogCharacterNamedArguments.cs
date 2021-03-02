using Discord.Commands;
using System;

namespace DnDDiscordBot.Models
{
    [NamedArgumentType]
    public class LevelLogCharacterNamedArguments
    {
        public string Name { get; set; }
        public string Levels { get; set; }

        public int Low => int.Parse(Levels.Split(',')[0]);
        public int High => int.Parse(Levels.Split(',')[1]);
    }
}
