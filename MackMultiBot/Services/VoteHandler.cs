using MackMultiBot.Data;
using MackMultiBot.Interfaces;

namespace MackMultiBot.Services
{
	public class VoteHandler(ILobby lobby) : IVoteHandler
	{
		List<IVote> _votes = [];

		public IVote FindOrCreateVote(string name, string detailedName)
		{
			var vote = _votes.FirstOrDefault(x => x.Name == name);

			if (vote == null)
			{
				vote = new Vote(name, detailedName, lobby);
				_votes.Add(vote);
			}

			return vote;
		}
	}
}
