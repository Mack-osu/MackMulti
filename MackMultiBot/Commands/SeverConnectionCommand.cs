using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Commands
{
	// Temporary command for testing bancho reconnections.
	public class SeverConnectionCommmand : ICommand
	{
		public string Command => "severconnection";

		public string[]? Aliases => [];

		public int MinimumArguments => 0;

		public bool IsGlobal => false;

		public bool AdminCommand => true;

		public string Usage => string.Empty;

		public Task Execute(CommandContext ctx) => Task.CompletedTask;
	}
}
