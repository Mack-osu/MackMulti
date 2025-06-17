using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class RemoveAdminCommand : ICommand
	{
		public string Command => "removeadmin";

		public string[]? Aliases => ["unadmin"];

		public int MinimumArguments => 1;

		public bool IsGlobal => false;

		public bool AdminCommand => false;

		public string Usage => "!admin <username>";

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
