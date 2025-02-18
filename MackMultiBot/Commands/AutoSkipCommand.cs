using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class AutoSkipCommand : ICommand
	{
		public string Command => "autoskip";

		public string[]? Aliases => ["as"];

		public int MinimumArguments => 0;

		public bool IsGlobal => false;

		public bool AdminCommand => false;

		public string Usage => "!autoskip <enable/disable>";

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
