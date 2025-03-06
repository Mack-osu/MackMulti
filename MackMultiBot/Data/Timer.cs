using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITimer = MackMultiBot.Interfaces.ITimer;

namespace MackMultiBot.Data
{
	public class Timer(ITimerHandler timerHandler, string name) : ITimer
	{
		public string Name { get; init; } = name;
		public bool IsActive { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }

		public ITimerHandler TimerHandler { get; init; } = timerHandler;

		public int ReminderTime { get; set; }

		public void Start(TimeSpan duration, int reminderTime = 0)
		{
			if (!IsActive)
				Logger.Log(LogLevel.Trace, $"Timer: Starting timer '{Name}' with a duration of '{duration}'");

			StartTime = DateTime.UtcNow;
			EndTime = StartTime + duration;
			ReminderTime = reminderTime;

			IsActive = true;
		}

		public void Delay(TimeSpan duration) => EndTime += duration;

		public void Stop()
		{
			Logger.Log(LogLevel.Info, $"Timer: Stopping timer '{Name}'");
			IsActive = false;
		}
	}
}
