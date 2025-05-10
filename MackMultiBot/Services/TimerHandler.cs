using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using ITimer = MackMultiBot.Interfaces.ITimer;
using Timer = MackMultiBot.Data.Timer;

namespace MackMultiBot
{
	public class TimerHandler(ILobby lobby) : ITimerHandler
	{
		List<ITimer> _timers = [];

		bool _isRunning;
		Task? _timerEventPumpTask;

		public ITimer FindOrCreateTimer(string name)
		{
			var timer = _timers.FirstOrDefault(t => t.Name == name);

			if (timer != null)
			{
				return timer;
			}

			timer = new Timer(this, name);

			_timers.Add(timer);

			return timer;
		}

		public Task Start()
		{
			_isRunning = true;
			_timerEventPumpTask = Task.Run(TimerPump);
			return Task.CompletedTask;
		}

		public Task Stop()
		{
			_isRunning = false;
			return Task.CompletedTask;
		}

		async Task TimerPump()
		{
			Logger.Log(LogLevel.Trace, "TimerHandler: Pump started");

			while (_isRunning)
			{
				// Reminders
				foreach (var timer in _timers.Where(x => x.IsActive && x.ReminderTime != 0 && DateTime.UtcNow >= x.EndTime.AddSeconds(-x.ReminderTime)).ToList())
				{
					Logger.Log(LogLevel.Info, $"TimerHandler: Reminder of timer '{timer.Name}' triggered");

					timer.ReminderTime = 0;


					if (lobby.BehaviorEventProcessor == null)
					{
						Logger.Log(LogLevel.Warn, "TimerHandler: Lobby BehaviorEventProcessor is null, cannot trigger reminder event.");
						continue;
					}

					await lobby.BehaviorEventProcessor.OnTimerReminderEvent(timer);
				}

				// Finished Timers
				foreach (var timer in _timers.Where(x => x.IsActive && DateTime.UtcNow >= x.EndTime).ToList())
				{
					Logger.Log(LogLevel.Info, $"TimerHandler: Timer '{timer.Name}' finished");

					timer.IsActive = false;

					if (lobby.BehaviorEventProcessor == null)
					{
						Logger.Log(LogLevel.Warn, "TimerHandler: Lobby BehaviorEventProcessor is null, cannot trigger finished event.");
						continue;
					}

					await lobby.BehaviorEventProcessor.OnTimerFinishedEvent(timer);
				}

				await Task.Delay(1000);
			}

			Logger.Log(LogLevel.Trace, "TimerHandler: Pump stopped");
		}
	}
}
