using BanchoSharp.Multiplayer;
using OsuSharp.Models.Scores;

namespace MackMultiBot.Data
{
	public class ScoreResult(MultiplayerPlayer player, Score? score)
	{
		public MultiplayerPlayer Player { get; } = player;

		public Score? Score { get; } = score;
	}
}
