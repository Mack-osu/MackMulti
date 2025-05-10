namespace MackMultiBot.Interfaces
{
	public interface IVoteHandler
	{
		public IVote FindOrCreateVote(string name, string detailedName);
	}
}
