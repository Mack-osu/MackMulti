using BanchoSharp.Interfaces;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;

namespace MackMultiBot.Data
{
	public class Vote(string name, string detailedName, ILobby lobby) : IVote
	{
		public string Name => name;

		public bool IsActive { get; set; }

		public List<string> Votes { get; set; } = [];

		public bool AddPlayerVote(IMultiplayerPlayer player)
		{
			if (lobby == null) return false;

			// Start new vote
			if (!IsActive)
			{
				IsActive = true;
				Votes.Clear();
			}

			RemoveIrrelevantVotes();

			if (!Votes.Contains(player.Name))
				Votes.Add(player.Name);

			int requiredVotes = Math.Max(lobby.MultiplayerLobby!.Players.Count / 2 + 1, 1);

			if (Votes.Count >= requiredVotes)
			{
				Logger.Log(LogLevel.Info, $"Vote: Passed vote {Name} successfully");

				lobby.BanchoConnection.MessageHandler.SendMessage(lobby.MultiplayerLobby!.ChannelName, $"{detailedName} vote passed! ({Votes.Count}/{requiredVotes})");

				IsActive = false;
				Votes.Clear();

				return true;
			}

			lobby.BanchoConnection.MessageHandler.SendMessage(lobby.MultiplayerLobby!.ChannelName, $"{detailedName} vote ({Votes.Count}/{requiredVotes})");

			return false;
		}

		public void Abort()
		{
			Logger.Log(LogLevel.Trace, $"Vote: Aborted vote {name} with {Votes.Count} vote(s)");
			IsActive = false;
			Votes.Clear();
		}

		void RemoveIrrelevantVotes()
		{
			if (lobby.MultiplayerLobby == null) return;

			foreach (var vote in Votes.ToList().Where(vote => lobby.MultiplayerLobby.Players.All(x => x.Name != vote)))
			{
				Logger.Log(LogLevel.Trace, $"Vote: Removed player '{Name}' from vote {vote}, player disconnected");

				Votes.Remove(vote);
			}
		}
	}
}
