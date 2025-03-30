using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using System.Reflection;
using System.Threading.Channels;
using ITimer = MackMultiBot.Interfaces.ITimer;

namespace MackMultiBot
{
	public class BehaviorEventProcessor(ILobby lobby)
	{
		List<BotBehaviorEvent> _events = [];
		List<string> _registeredBehaviors = [];

		private readonly Dictionary<string, Channel<EventExecution>> _eventChannels = new();
		private readonly List<Task> _pumps = [];

		public void RegisterBehavior(string behavior)
		{
			var behaviorType = AppDomain.CurrentDomain.
				GetAssemblies().
				SelectMany(s => s.GetTypes()).
				FirstOrDefault(t => typeof(IBehavior).IsAssignableFrom(t) && t.IsClass && t.Name == behavior);

			if (behaviorType == null)
			{
				Logger.Log(LogLevel.Error, $"BehaviorEventProcessor: Attempted to register nonexistant behavior '{behavior}'");
				throw new InvalidOperationException($"BehaviorEventProcessor: Attempted to register nonexistant behavior '{behavior}'");
			}

			var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

			foreach (var method in methods)
			{
				var ignoreArgs = method.GetParameters().Length == 0;

				var botEventAttribute = method.GetCustomAttribute<BotEvent>();
				if (botEventAttribute != null)
				{
					_events.Add(new BotBehaviorEvent(behavior, method, behaviorType, ignoreArgs, botEventAttribute.Type, botEventAttribute.OptionalArgument));
				}
			}

			// Create an instance of the behavior class.
			Activator.CreateInstance(behaviorType, new BehaviorEventContext(lobby));
			_registeredBehaviors.Add(behavior);

			Logger.Log(LogLevel.Info, $"BehaviorEventProcessor: Successfully registered behavior '{behavior}'");
		}
	
		public void Start()
		{
			if (lobby.MultiplayerLobby == null)
				throw new InvalidOperationException("BehaviorEventProcessor: Cannot start processor while MutiplayerLobby is null");

			lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
			lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
			lobby.MultiplayerLobby.OnMatchAborted += OnMatchAborted;
			lobby.MultiplayerLobby.OnPlayerJoined += OnPlayerJoined;
			lobby.MultiplayerLobby.OnPlayerDisconnected += OnPlayerDisconnected;
			lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
			lobby.MultiplayerLobby.OnHostChangingMap += OnHostChangingMap;
			lobby.MultiplayerLobby.OnAllPlayersReady += OnAllPlayersReady;
			lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;

			lobby.BanchoConnection.MessageHandler.OnMessageReceived += OnMessageReceived;

			foreach (var behavior in _registeredBehaviors)
			{
				var channel = Channel.CreateUnbounded<EventExecution>();

				_eventChannels.Add(behavior, channel);
				_pumps.Add(Task.Run(() => BehaviorEventPump(channel.Reader, behavior)));
			}
		}

		public void Stop()
		{
			if (lobby.MultiplayerLobby == null)
				throw new InvalidOperationException("BehaviorEventProcessor: Cannot stop processor while MutiplayerLobby is null");

			lobby.MultiplayerLobby.OnMatchStarted -= OnMatchStarted;
			lobby.MultiplayerLobby.OnMatchFinished -= OnMatchFinished;
			lobby.MultiplayerLobby.OnMatchAborted -= OnMatchAborted;
			lobby.MultiplayerLobby.OnPlayerJoined -= OnPlayerJoined;
			lobby.MultiplayerLobby.OnPlayerDisconnected -= OnPlayerDisconnected;
			lobby.MultiplayerLobby.OnHostChanged -= OnHostChanged;
			lobby.MultiplayerLobby.OnHostChangingMap -= OnHostChangingMap;
			lobby.MultiplayerLobby.OnAllPlayersReady -= OnAllPlayersReady;
			lobby.MultiplayerLobby.OnBeatmapChanged -= OnBeatmapChanged;

			lobby.BanchoConnection.MessageHandler.OnMessageReceived -= OnMessageReceived;

			foreach (var channel in _eventChannels.Values)
			{
				channel.Writer.Complete();
			}

			Task.WhenAll(_pumps).Wait();
		}

		#region Bancho Events

		private async void OnMatchStarted()
		{
			await ExecuteBotCallback(BotEventType.MatchStarted);
		}

		private async void OnMatchFinished()
		{
			await ExecuteBotCallback(BotEventType.MatchFinished);
		}

		private async void OnMatchAborted()
		{
			await ExecuteBotCallback(BotEventType.MatchAborted);
		}

		private async void OnPlayerJoined(IMultiplayerPlayer player)
		{
			await ExecuteBotCallback(BotEventType.PlayerJoined, player);
		}

		private async void OnPlayerDisconnected(PlayerDisconnectedEventArgs eventArgs)
		{
			await ExecuteBotCallback(BotEventType.PlayerDisconnected, eventArgs.Player);
		}

		private async void OnHostChanged(IMultiplayerPlayer player)
		{
			await ExecuteBotCallback(BotEventType.HostChanged, player);
		}

		private async void OnHostChangingMap()
		{
			await ExecuteBotCallback(BotEventType.HostChangingMap);
		}

		private async void OnAllPlayersReady()
		{
			await ExecuteBotCallback(BotEventType.AllPlayersReady);
		}

		private async void OnBeatmapChanged(BeatmapShell beatmapshell)
		{
			await ExecuteBotCallback(BotEventType.MapChanged, beatmapshell);
		}

		private async void OnMessageReceived(IPrivateIrcMessage msg)
		{
			//if (msg.Recipient != lobby.MultiplayerLobby?.ChannelName)
			//{
			//	if (msg.IsBanchoBotMessage)
			//	{
			//		await ExecuteBotCallback(BotEventType.BanchoBotMessageReceived, msg);
			//	}

			//	return;
			//}

			await ExecuteBotCallback(BotEventType.MessageReceived, msg);
		}

		#endregion

		#region Bot Events
		public async Task OnBehaviorEvent(string name)
		{
			await ExecuteBotCallback(BotEventType.BehaviorEvent, name);
		}

		public async Task OnCommandExecuted(string command, CommandContext commandContext)
		{
			await ExecuteBotCallbackWithArgument(BotEventType.Command, command, commandContext);
		}

		public async Task OnInitializeEvent()
		{
			await ExecuteBotCallback(BotEventType.Initialize);
		}

		public async Task OnTimerReminderEvent(ITimer timer)
		{
			await ExecuteBotCallbackWithArgument(BotEventType.TimerReminder, timer.Name, timer);
		}

		public async Task OnTimerFinishedEvent(ITimer timer)
		{
			await ExecuteBotCallbackWithArgument(BotEventType.TimerFinished, timer.Name, timer);
		}

		async Task ExecuteBotCallback(BotEventType botEventType, object? param = null)
		{
			var events = _events.Where(x => x.BotEventType == botEventType);

			await ExecuteCallback(events.ToList(), param);
		}

		public async Task ExecuteBotCallbackWithArgument(BotEventType botEventType, string argument, object? param = null)
		{
			var events = _events.Where(x => x.BotEventType == botEventType && x.OptionalArgument?.ToLower() == argument.ToLower());

			await ExecuteCallback(events.ToList(), param);
		}

		#endregion

		async Task ExecuteCallback<T>(IEnumerable<T> behaviorEvents, object? param = null) where T : BotBehaviorEvent
		{
			foreach (var behaviorEvent in behaviorEvents)
			{
				var channel = _eventChannels[behaviorEvent.Name];

				await channel.Writer.WriteAsync(new EventExecution(behaviorEvent, param));
			}
		}

		async Task BehaviorEventPump(ChannelReader<EventExecution> reader, string behaviorName)
		{
			Logger.Log(LogLevel.Trace, $"BehaviorEventPorcessor: Starting pump for behavior '{behaviorName}'");

			while (await reader.WaitToReadAsync())
			{
				while (reader.TryRead(out var eventExecution))
				{
					var behaviorEvent = eventExecution.BehaviorEvent;

					var instance = Activator.CreateInstance(behaviorEvent.BehaviorType, new BehaviorEventContext(lobby)) as IBehavior;

					try
					{
						var eventExecuteItem = eventExecution;

						var executeEventTask = Task.Run(async () =>
						{
							// Invoke the method on the behavior class instance
							var methodTask = behaviorEvent.Method.Invoke(instance, behaviorEvent.IgnoreArguments ? [] : [eventExecuteItem.Param]);

							// If we have a return value, it's a task, so await it
							if (methodTask != null)
								await (Task)methodTask;

							// If the behavior class implements IBehaviorDataConsumer, save the data
							if (instance is IBehaviorDataConsumer dataBehavior)
								await dataBehavior.SaveData();
						});

						await Task.WhenAny(executeEventTask, Task.Delay(TimeSpan.FromSeconds(15)));

						if (!executeEventTask.IsCompleted)
							Logger.Log(LogLevel.Error, $"BehaviorEventProcessor: Timeout while executing callback {behaviorEvent.Name}.{behaviorEvent.Method.Name}()");
					}
					catch (Exception e)
					{
						Logger.Log(LogLevel.Error, $"BehaviorEventProcessor: Exception while executing callback {behaviorEvent.Name}.{behaviorEvent.Method.Name}(), {e}");
					}
				}
			}

			Logger.Log(LogLevel.Trace, $"BehaviorEventPorcessor: Pump stopped for behavior '{behaviorName}'");
		}

		record EventExecution(BotBehaviorEvent BehaviorEvent, object? Param);

		class BotBehaviorEvent(string name, MethodInfo method, Type behaviorType, bool ignoreArgs, BotEventType botEventType, string? optionalArgument)
		{
			public string Name { get; } = name;

			public MethodInfo Method { get; } = method;

			public bool IgnoreArguments { get; } = ignoreArgs;

			public Type BehaviorType { get; } = behaviorType;

			public BotEventType BotEventType { get; } = botEventType;

			public string? OptionalArgument { get; } = optionalArgument;
		}
	}
}
