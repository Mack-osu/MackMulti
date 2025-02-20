using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class SetHostCommand : ICommand
	{
		public string Command => "sethost";

		public string[]? Aliases => ["sh"];

		public int MinimumArguments => 1;

		public bool IsGlobal => false;

		public bool AdminCommand => true;

		public string Usage => string.Empty;

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
