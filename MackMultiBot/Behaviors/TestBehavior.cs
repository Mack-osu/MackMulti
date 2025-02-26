using MackMultiBot.Interfaces;

namespace MackMultiBot.Behaviors
{
	public class TestBehavior(BehaviorEventContext context) : IBehavior
	{
		[BotEvent(BotEventType.Command, "test")]
		public void TestCommand(CommandContext commandContext)
		{
			commandContext.Reply($"Lobby Players: {string.Join(", ", commandContext.Lobby?.MultiplayerLobby?.Players.Select(x => x.Name + $"[{x.Id}]").ToList()!)}");
		}
	}
}
