using BanchoSharp.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Database.Entities
{
	public class LobbyRuleConfiguration
	{
		public int Id { get; set; }
		public int LobbyConfigurationId { get; set; }

		public bool LimitDifficulty { get; set; } = true;
			public float MinimumDifficulty { get; set; } = 0;
			public float MaximumDifficulty { get; set; } = float.PositiveInfinity;
				public float? DifficultyMargin { get; set; } = 0;

		public bool LimitMapLength { get; set; } = true;
			public int MinimumMapLength { get; set; } = 0;
			public int MaximumMapLength { get; set; } = 600;
	}
}
