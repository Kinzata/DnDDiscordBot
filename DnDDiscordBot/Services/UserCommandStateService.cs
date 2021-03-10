using Discord.WebSocket;
using DnDDiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DnDDiscordBot.Services
{
    public class UserCommandStateService
    {
        private Dictionary<ulong, CommandState> _commandState;

        public UserCommandStateService()
        {
            _commandState = new Dictionary<ulong, CommandState>();
        }

        public CommandState BeginQuest(ulong userId)
        {
            _commandState[userId] = CommandState.QuestCreation;
            return _commandState[userId];
        }

        public CommandState GetCommandStateForUser(ulong userId)
        {
            if( _commandState.ContainsKey(userId))
            {
                var state = _commandState[userId];

                return state;
            }
            else
            {
                return InitForUser(userId);
            }

        }

        public CommandState GetCommandStateForUser(SocketUser user)
        {
            return GetCommandStateForUser(user.Id);
        }

        public CommandState InitForUser(ulong userId)
        {
            _commandState[userId] = CommandState.None;

            return CommandState.None;
        }

        public CommandState InitForUser(SocketUser user)
        {
            return InitForUser(user.Id);
        }

    }
}
