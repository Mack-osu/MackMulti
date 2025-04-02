using BanchoSharp.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Database.Entities
{
	public class LobbyConfiguration
	{
		public string Name { get; set; } = "Unnamed";

		public LobbyFormat? TeamMode { get; set; } = LobbyFormat.HeadToHead;

		public WinCondition? ScoreMode { get; set; } = WinCondition.Score;

		public string[]? Mods { get; set; }

		public int? Size { get; set; } = 16;

		public string? Password { get; set; } = string.Empty;

		public LobbyRuleConfiguration RuleConfig { get; set; } = new();
	}
}
