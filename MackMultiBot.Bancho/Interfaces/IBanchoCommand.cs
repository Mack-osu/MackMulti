using MackMultiBot.Bancho.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Interfaces
{

	public interface IBanchoCommand
	{
		public static abstract string Command { get; }

		/// <summary>
		/// List of responses that are considered successful.
		/// </summary>
		public static abstract IReadOnlyList<CommandResponse> SuccessfulResponses { get; }
	}
}
