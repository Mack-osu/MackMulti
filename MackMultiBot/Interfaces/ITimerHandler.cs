using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Interfaces
{
	public interface ITimerHandler
	{
		public ITimer FindOrCreateTimer(string name);

		public Task Start();
		public Task Stop();
	}
}
