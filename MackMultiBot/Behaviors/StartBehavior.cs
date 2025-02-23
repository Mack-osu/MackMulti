using BanchoSharp.Multiplayer;
using MackMulti.Database.Databases;
using MackMultiBot.Bancho;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class StartBehavior(BehaviorEventContext context) : IBehavior
	{
		static NLog.Logger _logger = NLog.LogManager.GetLogger("StartBehaviorLogger");


		[BotEvent(BotEventType.Command, "start")]
		public void OnStartCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

			if (commandContext.Player.Name != context.Lobby?.MultiplayerLobby?.Host?.Name)
				return;

			if (commandContext.Args.Length == 0)
			{
				commandContext.Reply($"!mp start");
				return;
			}

			// Try to parse into start timer
			if (!int.TryParse(commandContext.Args[0], out int result))
			{
				commandContext.Reply($"Invalid argument, usage: {commandContext.Command.Usage}");
				return;
			}

			if (result > 60)
			{
				commandContext.Reply("Start timer must be below 60 seconds");
				return;
			}
			commandContext.Reply($"!mp start {result}");
		}
	}
}
