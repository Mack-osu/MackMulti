using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Interfaces
{
	public interface ICommand
	{
		public string Command { get; }
		public string[]? Aliases { get; }
		public int MinimumArguments { get; }
		public bool IsGlobal { get; }
		public bool AdminCommand { get; }
		public string Usage { get; }

		public Task Execute(CommandContext ctx);
	}
}
