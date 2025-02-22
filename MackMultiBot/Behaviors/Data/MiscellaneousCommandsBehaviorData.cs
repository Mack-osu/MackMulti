using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors.Data
{
	public class MiscellaneousCommandsBehaviorData : IBehaviorData
	{
		public List<PlayerJoinRecord> PlayerTimeRecords { get; set; } = [];

		public record PlayerJoinRecord
		{
			public string PlayerName { get; init; } = string.Empty;

			public DateTime JoinTime { get; init; }
		}
	}
}
