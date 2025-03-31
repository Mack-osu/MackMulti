using MackMultiBot.Behaviors.Data;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;

namespace MackMultiBot.Behaviors
{
	public class LobbyWatchBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		private readonly BehaviorDataProvider<LobbyWatchBehaviorData> _dataProvider = new(context.Lobby);
		private LobbyWatchBehaviorData Data => _dataProvider.Data;
		public async Task SaveData() => await _dataProvider.SaveData();

		[BotEvent(BotEventType.Initialize)]
		public void OnInitialize()
		{
			Data.RecentEventTime = DateTime.UtcNow;
			context.Lobby.TimerHandler?.FindOrCreateTimer("LobbyWatchTimer").Start(TimeSpan.FromMinutes(5));
		}

		[BotEvent(BotEventType.TimerFinished, "LobbyWatchTimer")]
		public void OnWatchTimerElapsed()
		{
			if ((DateTime.UtcNow - Data.RecentEventTime).TotalHours >= 0.75)
			{
				Logger.Log(LogLevel.Warn, "No lobby events in the past 45 minutes, attempting restart.");

				var lobby = context.Lobby;

				_ = Task.Run(async () =>
				{
					try
					{
						// Shoot an mp close into the void to make sure no duplicate lobbies are running before relaunching a new one
						context.SendMessage("!mp close");
						lobby.RemoveInstance();
						await lobby.ConnectOrCreateAsync(true);
					}
					catch (Exception e)
					{
						Logger.Log(LogLevel.Error, $"LobbyWatchBehavior: Failed to re-create to the lobby. Exception: {e}");
					}
				});

				return;
			}

			context.Lobby.TimerHandler?.FindOrCreateTimer("LobbyWatchTimer").Start(TimeSpan.FromMinutes(5));
		}

		#region Events

		[BotEvent(BotEventType.PlayerJoined)]
		public void OnPlayerJoined() => Data.RecentEventTime = DateTime.UtcNow;

		[BotEvent(BotEventType.PlayerDisconnected)]
		public void OnPlayerDisconnected() => Data.RecentEventTime = DateTime.UtcNow;

		[BotEvent(BotEventType.MatchFinished)]
		public void OnMatchFinished() => Data.RecentEventTime = DateTime.UtcNow;

		#endregion
	}
}
