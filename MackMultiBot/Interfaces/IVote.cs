using BanchoSharp.Interfaces;

namespace MackMultiBot.Interfaces
{
	public interface IVote
	{
		public string Name { get; }
		public bool IsActive { get; set; }

		public List<string> Votes { get; set; }

		public bool AddPlayerVote(IMultiplayerPlayer player);

		public void Abort();
	}
}
