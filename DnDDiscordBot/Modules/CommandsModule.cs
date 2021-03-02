using Discord;
using Discord.Commands;
using DnDDiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Modules
{
    [Group("timbly")]
    [Alias("tim", "registry")]
    public class CommandsModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _commandService;

        public CommandsModule(CommandService commandService)
        {
            _commandService = commandService;
        }

        [Command("help")]
        [Summary("This command.")]
        public async Task HelpAsync()
        {
            List<CommandInfo> commands = _commandService.Commands.ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();

            try
            {
                foreach (CommandInfo command in commands)
                {
                    var arguments = command.Parameters;
                    var argumentEmbedText = "";
                    if( arguments.Count > 0)
                    {
                        var name = arguments[0].Name;
                        if (name == "args")
                        {
                            name = "args...";
                            argumentEmbedText = $" [{name}]";
                        }
                        else
                        {
                            argumentEmbedText = $" <{name}>";
                        }
                    }

                    // Get the command Summary attribute information
                    string embedFieldText = command.Summary;
                    if( string.IsNullOrEmpty(embedFieldText))
                    {
                        embedFieldText = "No description available\n";
                    }

                    embedBuilder.AddField(command.Name + argumentEmbedText, embedFieldText);
                }
            }
            catch( Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
        }

    }
}
