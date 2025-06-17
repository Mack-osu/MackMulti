using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class SetAdminCommand : ICommand
	{
		public string Command => "admin";

		public string[]? Aliases => ["addadmin", "setadmin"];

		public int MinimumArguments => 1;

		public bool IsGlobal => false;

		public bool AdminCommand => false;

		public string Usage => "!admin <username>";

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
