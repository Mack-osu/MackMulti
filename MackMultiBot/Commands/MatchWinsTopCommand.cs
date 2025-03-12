using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class MatchWinsTopCommand : ICommand
	{
		public string Command => "matchwinstop";

		public string[]? Aliases => ["wintop", "winstop", "mwtop", "topwins", "topmw", "wtop"];

		public int MinimumArguments => 0;

		public bool IsGlobal => false;

		public bool AdminCommand => false;

		public string Usage => string.Empty;

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
