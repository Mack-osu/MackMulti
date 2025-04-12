using MackMultiBot.Interfaces;

namespace MackMultiBot.Behaviors
{
	public class TestBehavior(BehaviorEventContext context) : IBehavior
	{
		/// <summary>
		/// DIsplays all players in lobby, including their osu! user ID.
		/// </summary>
		[BotEvent(BotEventType.Command, "test")]
		public void OnTestCommand(CommandContext commandContext)
		{
			commandContext.Reply($"Lobby Players: {string.Join(", ", commandContext.Lobby?.MultiplayerLobby?.Players.Select(x => x.Name + $"[{(x.Id == null ? "No ID" : x.Id)}]").ToList()!)}");
		}
	}
}
