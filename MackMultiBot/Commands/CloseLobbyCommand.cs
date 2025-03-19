using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class CloseLobbyCommand : ICommand
	{
		public string Command => "close";

		public string[]? Aliases => ["c", "closelobby"];

		public int MinimumArguments => 0;

		public bool IsGlobal => false;

		public bool AdminCommand => true;

		public string Usage => string.Empty;

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
