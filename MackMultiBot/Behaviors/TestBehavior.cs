using MackMultiBot.Interfaces;

namespace MackMultiBot.Behaviors
{
	public class TestBehavior(BehaviorEventContext context) : IBehavior
	{
		/// <summary>
		/// DIsplays all players in lobby, including their osu! user ID.
		/// </summary>
		[BotEvent(BotEventType.Command, "test")]
		public async void OnTestCommand(CommandContext commandContext)
		{
			//commandContext.Reply($"Lobby Players: {string.Join(", ", commandContext.Lobby?.MultiplayerLobby?.Players.Select(x => x.Name + $"[{(x.Id == null ? "No ID" : x.Id)}]").ToList()!)}");
			context.SendMessage("!mp close");
			context.Lobby.RemoveInstance();
			await context.Lobby.ConnectOrCreateAsync(true);

		}

		[BotEvent(BotEventType.Command, "testvote")]
		public void OnTestVoteCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null || context.Lobby.VoteHandler == null) return;

			if (context.Lobby.VoteHandler.FindOrCreateVote("Test", "testing vote").AddPlayerVote(commandContext.Player))
				commandContext.Reply("Wahoo vote passed");
		}
	}
}
