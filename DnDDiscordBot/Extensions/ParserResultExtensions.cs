using CommandLine;
using Discord;
using Discord.Commands;
using DnDDiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DnDDiscordBot.Extensions
{
    public static class ParserResultExtensions
    {
        /// <summary>
        /// Checks for any errors within the ParserResult and handles building and sending a message response back to Discord if
        /// the error is a HelpRequestedError or HelpVerbRequestedError
        /// </summary>
        /// <param name="result"></param>
        /// <param name="context">SocketCommandContext</param>
        public static async Task HandleHelpRequestedErrorAsync<T>(this ParserResult<T> result, SocketCommandContext context)
        {
            if (result.Errors.Count() > 0)
            {
                var helpError = result.Errors.Where(e => e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.HelpVerbRequestedError).FirstOrDefault();
                if (helpError != null)
                {
                    await HandleHelp(result, context);
                }
            }
        }

        private static async Task HandleHelp<T>(ParserResult<T> result, SocketCommandContext context)
        {
            string header;
            var description = "";

            var embedFieldList = new List<EmbedFieldBuilder>();

            // Attempt to load the class for the Options type
            var optionsTypeClass = result.TypeInfo.Current;

            // If it's not null then it's a command.
            if (optionsTypeClass != null && optionsTypeClass != typeof(NullInstance))
            {
                // If it has a Verb attribute, grab the help text for the description!
                var verb = optionsTypeClass.GetCustomAttribute<VerbAttribute>();
                if (verb != null)
                {
                    description = verb.HelpText;
                }

                // For now, header can be defined by a specific static field
                header = (string)optionsTypeClass.GetRuntimeProperty("HelpHeader").GetValue(null);

                // Iterate over each property, grabbing the Option attributes for info
                var propertiesInfo = optionsTypeClass.GetProperties();
                var optionHelpText = "";
                var subcommandHelpText = "";
                foreach (var prop in propertiesInfo)
                {
                    var optionAttribute = prop.GetCustomAttribute<OptionAttribute>();
                    if (optionAttribute != null && !optionAttribute.Hidden)
                    {
                        var subcommand = prop.GetCustomAttribute<SubcommandAttribute>();
                        if( subcommand != null)
                        {
                            subcommandHelpText += $"{optionAttribute.LongName} - {optionAttribute.HelpText}\n";
                        }
                        else
                        {
                            optionHelpText += optionAttribute.HelpText + "\n";
                        }
                    }
                }

                embedFieldList.Add(new EmbedFieldBuilder
                {
                    Name = "__Valid Attributes__",
                    Value = optionHelpText
                });

                embedFieldList.Add(new EmbedFieldBuilder
                {
                    Name = "__Subcommands__",
                    Value = subcommandHelpText
                });
            }
            else
            {
                // If it is null, then choices has the commands we need info for.
                header = "Commands";
                description = "Index of commands";

                var choices = result.TypeInfo.Choices;
                foreach (var choice in choices)
                {
                    var verb = choice.GetCustomAttribute<VerbAttribute>();
                    if (verb != null)
                    {
                        embedFieldList.Add(new EmbedFieldBuilder
                        {
                            Name = (string)choice.GetRuntimeProperty("HelpHeader").GetValue(null),
                            Value = $"{verb.HelpText}"
                        });
                    }
                }
            }

            embedFieldList.Add(new EmbedFieldBuilder
            {
                Name = "More Help",
                Value = "An underlined command signifies that the command has subcommands.\n" +
"Type `!timbly <command> --help` for more info on a command.\n" +
"In the case of subcommands: `!timbly <command> <subcommand> --help` for info on the subcommand."
            });

            var embed = new EmbedBuilder
            {
                Title = $"{header}",
                Footer = new EmbedFooterBuilder { Text = "Arguments surrounded in angled brackets (<args>) are mandatory, while those surrounded in square brackets ([args]) are optional. In either case, don't include the brackets." },
                Timestamp = DateTime.Now,
                Description = description
            };

            foreach (var field in embedFieldList)
            {
                embed.AddField(field);
            }

            await context.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
