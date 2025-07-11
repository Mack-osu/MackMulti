using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors.Data
{
	public class HostQueueBehaviorData : IBehaviorData
	{
		public List<string> Queue { get; set; } = [];
		public List<string> PlayersDisconnectedDuringMatch { get; set; } = [];
	}
}
