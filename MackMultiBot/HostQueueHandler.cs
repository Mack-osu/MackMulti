using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public class HostQueueHandler : IHandler
	{
		[Command("test")]
		public static void TestCommand(string[] args)
		{
			Console.WriteLine("HOSTQUEUEHANDLER: TEST COMMAND");
		}
	}
}
