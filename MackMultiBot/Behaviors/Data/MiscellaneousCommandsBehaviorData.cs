using BanchoSharp.Multiplayer;
using MackMultiBot.Data;
using MackMultiBot.Interfaces;
using OsuSharp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors.Data
{
	public class MiscellaneousCommandsBehaviorData : IBehaviorData
	{
		public BeatmapInformation? LastPlayedBeatmapInfo { get; set; }

		public List<PlayerTimeRecord> PlayerTimeRecords { get; set; } = [];

		public record PlayerTimeRecord
		{
			public string PlayerName { get; init; } = string.Empty;

			public DateTime TrackingStartTime { get; init; }
		}
	}
}
