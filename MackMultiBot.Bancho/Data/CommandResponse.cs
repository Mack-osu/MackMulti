using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Data
{
	public class CommandResponse
	{
		public string Message { get; set; } = string.Empty;

		public CommandResponseType Type { get; set; } = CommandResponseType.Exact;
	}

	public enum CommandResponseType
	{
		Exact,
		StartsWith,
		Contains,
	}
}
