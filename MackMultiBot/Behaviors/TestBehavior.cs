using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class TestBehavior(BehaviorEventContext context) : IBehavior
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("TestBehaviorLogger");

		[BotEvent(BotEventType.Command, "test")]
		public void TestCommand(CommandContext commandContext)
		{
			commandContext.Reply($"Lobby Players: {string.Join(", ", commandContext.Lobby?.MultiplayerLobby?.Players.Select(x => x.Name).ToList()!)}");
			_logger.Info("TestBehavior: Executing TestCommand");
		}
	}
}
