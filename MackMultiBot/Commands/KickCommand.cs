using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class KickCommand : ICommand
	{
		public string Command => "kick";

		public string[]? Aliases => ["k"];

		public int MinimumArguments => 1;

		public bool IsGlobal => false;

		public bool AdminCommand => true;

		public string Usage => string.Empty;

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
