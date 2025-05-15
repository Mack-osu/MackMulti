using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using ITimer = MackMultiBot.Interfaces.ITimer;

namespace MackMultiBot.Behaviors
{
	public class StartBehavior : IBehavior
	{
		private BehaviorEventContext _context;
		private ITimer? _autoStartTimer;

		public StartBehavior(BehaviorEventContext context)
		{
			_context = context;

			if (_context.Lobby.TimerHandler == null) return;

			_autoStartTimer = _context.Lobby.TimerHandler.FindOrCreateTimer("AutoStartTimer");
		}

		[BotEvent(BotEventType.Command, "start")]
		public void OnStartCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

			// Start vote
			if (commandContext.Player.Name != _context.Lobby?.MultiplayerLobby?.Host?.Name)
			{
				if (commandContext.Lobby?.VoteHandler != null)
					if (commandContext.Lobby.VoteHandler.FindOrCreateVote("start", "Start the match").AddPlayerVote(commandContext.Player))
						commandContext.Reply($"!mp start");

				return;
			}

			if (commandContext.Args.Length == 0)
			{
				commandContext.Reply($"!mp start");
				return;
			}

			// Try to parse into start timer
			if (!int.TryParse(commandContext.Args[0], out int result))
			{
				commandContext.Reply($"Invalid argument, usage: {commandContext.Command.Usage}");
				return;
			}

			if (result > 60)
			{
				commandContext.Reply("Start timer must be below 60 seconds");
				return;
			}

			_autoStartTimer?.Start(TimeSpan.FromSeconds(result), result > 30 ? 10 : 0);
			_context.SendMessage($"Match queued to start in {result} seconds, use !stop to abort");
		}

		[BotEvent(BotEventType.Command, "forcestart")]
		public void OnForceStartCommand(CommandContext commandContext)
		{
			if (commandContext.Args.Length == 0)
			{
				_autoStartTimer?.Stop();
				commandContext.Reply($"!mp start");
				return;
			}

			// Try to parse into start timer
			if (!int.TryParse(commandContext.Args[0], out int result))
			{
				commandContext.Reply($"Invalid argument, usage: {commandContext.Command.Usage}");
				return;
			}

			commandContext.Reply($"!mp start {result}");
		}

		[BotEvent(BotEventType.Command, "stop")]
		public void OnStopCommandExecuted(CommandContext commandContext)
		{
			// Make sure the player is the host or an admin
			if (!commandContext.User.IsAdmin)
				if (commandContext.Player != commandContext.Lobby?.MultiplayerLobby?.Host)
					return;

			_autoStartTimer?.Stop();
		}

		[BotEvent(BotEventType.Command, "abort")]
		public void OnAbortCommandExecuted(CommandContext commandContext)
		{
			// Make sure the player is the host or an admin
			if (!commandContext.User.IsAdmin)
				if (commandContext.Player != commandContext.Lobby?.MultiplayerLobby?.Host)
					return;

			commandContext.Reply("!mp abort");
		}

		[BotEvent(BotEventType.AllPlayersReady)]
		public void OnAllPlayersReady()
		{
			_autoStartTimer?.Stop();
			_context.SendMessage("All players ready, starting match");
			_context.SendMessage("!mp start");
		}

		[BotEvent(BotEventType.BehaviorEvent, "NewMap")]
		public void OnNewMap() => _autoStartTimer?.Start(TimeSpan.FromSeconds(90), 10);

		[BotEvent(BotEventType.TimerFinished, "AutoStartTimer")]
		public void OnAutoStartTimerElapsed()
		{
			if (_context.Lobby.MultiplayerLobby?.Players.Count == 0)
			{
				Logger.Log(LogLevel.Info, "StartBehavior: No players in lobby, ignoring autostart timer");
				return;
			}

			_context.SendMessage("!mp start");
		}

		[BotEvent(BotEventType.TimerReminder, "AutoStartTimer")]
		public void OnAutoStartTimerReminder() => _context.SendMessage("Match starting in 10 seconds, use !stop to abort");

		private void AbortTimer() => _autoStartTimer?.Stop();

		[BotEvent(BotEventType.MatchStarted)]
		public void OnMatchStarted() => AbortTimer();

		[BotEvent(BotEventType.HostChanged)]
		public void OnHostChanged() => AbortTimer();

		[BotEvent(BotEventType.HostChangingMap)]
		public void OnHostChangingMap() => AbortTimer();
	}
}
