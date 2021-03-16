using Discord;
using Discord.Commands;
using DnDDiscordBot.Models;
using DnDDiscordBot.Services;
using System;
using System.Threading.Tasks;

namespace DnDDiscordBot.Submodules
{

    // TODO: Port to Command format
    public partial class QuestModule : ModuleBase<SocketCommandContext>
    {
        private UserCommandStateService _userCommandStateService;

        public QuestModule(UserCommandStateService userCommandStateService)
        {
            _userCommandStateService = userCommandStateService;
        }

        [Command("create")]
        [Summary("Begins the workflow for creating a quest.")]
        public async Task CreateAsync()
        {

            var commandState = _userCommandStateService.GetCommandStateForUser(Context.User.Id);

            if (commandState != CommandState.None)
            {
                // begin process for resetting state

                // TEMP - just reset
                _userCommandStateService.BeginQuest(Context.User.Id);
            }

            _userCommandStateService.BeginQuest(Context.User.Id);

            await Context.Message.AddReactionAsync(new Emoji("✅"));

            var embed = new EmbedBuilder
            {
                Title = $"Timbly's Quest Registration",
                Footer = new EmbedFooterBuilder { Text = "The law offices of Timbly, Timbly, Timbly and Associates, is not to be held responsible for any accidental or intentional death, dismemberment, or untimely banishment from the prime material plane that may happen while on an adventure. This is in accordance with Title Section \"Fables Player Contracts\",  subsection(B) paragraphs 1 thru 7." },
                Description = "Please use the template below, filling out the entire form:",
                Timestamp = DateTime.Now
            };

            string template = @"
```
Quest Name:
Recommended Levels: #-#
Player Count: #-#
Type:
```
";

            embed.AddField("Please use the template below:", template);

            await Context.User.SendMessageAsync(embed: embed.Build());
        }
    }
}
