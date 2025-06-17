using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors.Data
{
	public class LobbyManagerBehaviorData : IBehaviorData
	{
		public bool IsFreshInstance { get; set; }

		public string[] NoticeStrings { get; set; } = [
			"Ever wanted to host your own lobby? Version 1.0 just released on my [https://github.com/Mack-osu/MackMulti GitHub]",
			"Would you like to run your own host rotate bot? Version 1.0 just released on my [https://github.com/Mack-osu/MackMulti GitHub]",
			"Curious what it's like to run your own bot? Version 1.0 just released on my [https://github.com/Mack-osu/MackMulti GitHub]",
			"Match ever get stuck waiting for players? Try !abort",
			"This lobby tracks all sorts of statistics, check out !totalplaytime",
			"All available commands can be found [https://github.com/Mack-osu/MackMulti/blob/main/COMMANDS.md here]",
			"Use !rules to remind everyone of the current lobby settings.",
			"Check your spot in queue with !queueposition (or !qp)",
			"Admins can override beatmap rules with !overriderules.",
			"Want to suggest a new feature or command? Send a DM to [ Mack ] on osu!",
		];
	}
}
