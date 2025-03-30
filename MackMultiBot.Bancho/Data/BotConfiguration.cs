using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Data
{
	public class BotConfiguration
	{
		public string IrcUsername { get; set; } = string.Empty;
		public string IrcPassword { get; set; } = string.Empty;
		public string ApiClientId { get; set; } = string.Empty;
		public string ApiClientSecret { get; set; } = string.Empty;
		public string DatabaseDirectory { get; set; } = string.Empty;
		public string LogDirectory { get; set; } = string.Empty;
		public string LobbyName { get; set; } = string.Empty;
		public string LobbyIdentifier { get; set; } = string.Empty;
		public int TeamMode { get; set; }
		public int ScoreMode { get; set; }
		public string[] Mods { get; set; } = [];
		public int Size { get; set; }
		public string Password { get; set; } = string.Empty;
		public bool LimitDifficulty { get; set; }
		public bool LimitMapLength { get; set; }
		public float MinimumDifficulty { get; set; }
		public float MaximumDifficulty { get; set; }
		public float DifficultyMargin { get; set; }
		public int MinimumMapLength { get; set; }
		public int MaximumMapLength { get; set; }
	}
}
