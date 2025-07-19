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

		// This should not be here but I'm lazy :3
		public string[] NoticeStrings { get; set; } = [
			"Would you like to run your own host rotate lobby? The latest version of the bot can be found [https://github.com/Mack-osu/MackMulti here]",
			"Match ever get stuck waiting for players? Try !abort",
			"This lobby tracks all sorts of statistics, check out !totalplaytime",
			"All available commands can be found [https://github.com/Mack-osu/MackMulti/blob/main/COMMANDS.md here]",
			"Use !rules to remind everyone of the current lobby settings.",
			"Check your spot in queue with !queueposition (or !qp)",
			"Admins can override lobby rules with !overriderules.",
			"Want to suggest a new feature or command? Send a DM to [ Mack ] on osu!",
			"Joined in the middle of a match? !timeleft shows how long you'll have to wait.",
			"Did you know you can use !timeleft ping to receive an invite when the next map finishes?",
			"Most commands have aliases, like !bestmapscore which can be shortened to !bms",
			"Curious how many hours you've sunk into this lobby? Try !playtime",
			"See how you stack up against other players with !playtimetop, !winstop or !playcounttop",
			"Having trouble downloading a map? Try one of the mirror links using !mirror"
		];
	}
}
