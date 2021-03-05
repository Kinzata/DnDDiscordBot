using Discord.Commands;
using System;

namespace DnDDiscordBot.Models
{
    [NamedArgumentType]
    public class LevelLogCharacterNamedArguments
    {
        public string name { get; set; }
        public string levels { get; set; }
        public bool? details { get; set; }

        private readonly char[] delimiters = { ',', '-' };

        public int low
        {
            get
            {
                var low = 0;
                var success = false;

                foreach (var delimiter in delimiters)
                {
                    success = int.TryParse(levels.Split(delimiter)[0], out low);
                    if (success) return low;
                }

                // Try a final parse to see if only one argument was sent in
                success = int.TryParse(levels, out low);
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
                    var split = levels.Split(delimiter);
                    if (split.Length != 2) continue;

                    success = int.TryParse(split[1], out high);
                    if (success) return high;
                }

                // Try a final parse to see if only one argument was sent in
                success = int.TryParse(levels, out high);
                if (success) return high;

                // If nothing can be parsed, error out
                throw new Exception("Unable to parse 'level' input.");
            }
        }
    }
}
