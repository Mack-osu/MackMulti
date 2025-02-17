using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class HostQueueBehavior(BehaviorEventContext context) : IBehavior
	{
		static NLog.Logger _logger = NLog.LogManager.GetLogger("HostQueueHandlerLogger");

		List<string> _queue;

	}
}
