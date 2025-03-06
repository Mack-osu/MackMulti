using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Interfaces
{
	public interface ITimer
	{
		public string Name { get; }
		public bool IsActive { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime EndTime { get; set; }

		public ITimerHandler TimerHandler { get; }

		public int ReminderTime { get; set; }

		public void Start(TimeSpan duration, int earlyWarning = 0);

		public void Delay(TimeSpan duration);

		public void Stop();
	}
}
