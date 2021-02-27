using DnDDiscordBot.Services;
using System;
using Xunit;

namespace DnDDiscordBotTests
{
    public class LevelLogServiceTests
    {
        [Theory]
        [InlineData("<@1234> as Darion gains enough XP to advance to level 8", 1234)]
        [InlineData("<@!1234> as Shrizyne Quavarn, got enough daily XP to hit level 3", 1234)]

        public void ParseOwnerIdFromContent(string content, ulong expectedTestResult)
        {
            var service = new LevelLogService();

            Assert.Equal(expectedTestResult, service.ParseOwnerIdFromContent(content));
        }

        [Theory]
        [InlineData("<@asdfsd> as Darion gains enough XP to advance to level 8", "Darion")]
        [InlineData("<@asdfsd> as Shrizyne Quavarn, got enough daily XP to hit level 3", "Shrizyne Quavarn")]
        [InlineData("<@asdfsd> as Riksen has 48167 xp to advance to next level ", "Riksen")]
        [InlineData("<@asdfsd> as St. Emillia gains 14,000 XP to advance to level 6!", "St. Emillia")]
        [InlineData("<@asdfsd> as Shrizyne Quavarn, pitfought her way straight to level 2", "Shrizyne Quavarn")]
        [InlineData("<@asdfsd> as Doden 'Moonreaper' gains 500 xp from a quest and advances to level 3", "Doden 'Moonreaper'")]
        [InlineData("<@!95703816439083008>  as Yuki gains 0 xp to advance to level 7\nAdvancing to a total level of: 7 in Keeper's Pet\nRolling HP: 5\nNew HP: 45 + 5 + 2 = 52\nNew/improvement class feature: Nothing", "Yuki")]
        public void ParseCharacterNameFromContent(string content, string expectedTestResult)
        {
            var service = new LevelLogService();

            Assert.Equal(expectedTestResult, service.ParseCharacterNameFromContent(content));
        }

        [Theory]
        [InlineData("<@asdfsd> as Darion gains enough XP to advance to level 8", 8)]
        [InlineData("<@asdfsd> as Shrizyne Quavarn, got enough daily XP to hit level 3", 3)]
        [InlineData("<@asdfsd> as St. Emillia gains 14,000 XP to advance to level 6!", 6)]
        [InlineData("<@asdfsd> as Shrizyne Quavarn, pitfought her way straight to Level 20", 20)]
        public void ParseLevelFromContent(string content, int expectedTestResult)
        {
            var service = new LevelLogService();

            Assert.Equal(expectedTestResult, service.ParseLevelFromContent(content));
        }
    }
}
