using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public class CommandContext(IPrivateIrcMessage message, string[] args, Bot bot, ICommand command, User user)
	{
		public IPrivateIrcMessage Message { get; private set; } = message;
		public string[] Args { get; private set; } = args;
		public ILobby? Lobby { get; set; }
		public Bot Bot { get; private set; } = bot;
		public User User { get; set; } = user;
		public ICommand Command { get; private set; } = command;
		public MultiplayerPlayer? Player { get; set; }

		public void Reply(string message)
		{
			string channel = Message.IsDirect ? Message.Sender : Message.Recipient;

			Bot.BanchoConnection.MessageHandler.SendMessage(channel, message);
		}
	}
}
