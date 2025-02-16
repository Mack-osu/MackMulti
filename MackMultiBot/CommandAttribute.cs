using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class CommandAttribute : Attribute
	{
		public string Command { get; }

		public CommandAttribute(string command)
		{
			Command = command;
		}
	}
}
