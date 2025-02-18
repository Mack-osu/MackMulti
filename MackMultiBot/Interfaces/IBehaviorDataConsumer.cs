using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Interfaces
{
	public interface IBehaviorDataConsumer
	{
		public Task SaveData();
	}
}
