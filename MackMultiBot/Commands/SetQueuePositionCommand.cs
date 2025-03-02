using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	public class SetQueuePositionCommand : ICommand
	{
		public string Command => "setqueueposition";

		public string[]? Aliases => ["sqp", "setqueuepos"];

		public int MinimumArguments => 2;

		public bool IsGlobal => false;

		public bool AdminCommand => false;

		public string Usage => "!SetQueuePosition <user_name> <position>";

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
