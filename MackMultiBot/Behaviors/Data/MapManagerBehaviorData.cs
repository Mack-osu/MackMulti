using MackMultiBot.Data;
using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors.Data
{
	public class MapManagerBehaviorData : IBehaviorData
	{
		public BeatmapInformation BeatmapInfo { get; set; } = new BeatmapInformation();

		public int LastValidBeatmapId { get; set; } = 975342;
		public int LastBotAppliedBeatmapId { get; set; }

		public DateTime LastMatchStartTime { get; set; } = DateTime.UtcNow;

		public List<string> PlayersToPing { get; set; } = [];

		public bool RuleOverrideActive { get; set; }
	}
}
