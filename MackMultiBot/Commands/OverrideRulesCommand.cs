using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class OverrideRulesCommand : ICommand
	{
		public string Command => "overriderules";

		public string[]? Aliases => ["override", "or", "bypassrules", "bypass", "skipvalidation"];

		public int MinimumArguments => 0;

		public bool IsGlobal => false;

		public bool AdminCommand => true;

		public string Usage => string.Empty;

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
