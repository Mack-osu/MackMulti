using MackMultiBot.Interfaces;

namespace MackMultiBot.Behaviors
{
	public class TestBehavior(BehaviorEventContext context) : IBehavior
	{
		[BotEvent(BotEventType.Command, "test")]
		public void OnTestCommand(CommandContext commandContext)
		{
			commandContext.Reply($"Lobby Players: {string.Join(", ", commandContext.Lobby?.MultiplayerLobby?.Players.Select(x => x.Name + $"[{x.Id}]").ToList()!)}");
		}

		[BotEvent(BotEventType.Command, "severconnection")]
		public void OnSeverConnectionCommand(CommandContext commandContext)
		{
			commandContext.Lobby.ConnectOrCreateAsync();

			//commandContext.Bot.BanchoConnection._connectionWatch._tcpClient = null;
		}
	}
}
