using BanchoSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Interfaces
{
	public interface IBanchoConnection
	{
		public IBanchoClient? BanchoClient { get; }

		public bool IsConnected { get; }

		public Task StartAsync();

		public Task StopAsync();
	}
}