using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database;
using MackMultiBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MackMultiBot.Logging;
using MackMultiBot.Database.Databases;

namespace MackMultiBot.Behaviors
{
	public class HostQueueBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		readonly BehaviorDataProvider<HostQueueBehaviorData> _dataProvider = new(context.Lobby);
		private HostQueueBehaviorData Data => _dataProvider.Data;
		public async Task SaveData() => await _dataProvider.SaveData();

		#region Commands

		[BotEvent(BotEventType.Command, "autoskip")]
		public async Task OnAutoSkipCommand(CommandContext commandContext)
		{
			await using var userDb = new UserDb();

			if (commandContext.Player == null)
				return;

			var user = await userDb.FindOrCreateUser(commandContext.Player.Name);

			if (commandContext.Args.Length == 0)
			{
				commandContext.Reply($"User {commandContext.Player.Name}'s autoskip is currently {(user.AutoSkip ? "enabled" : "disabled")}");
				return;
			}

			user.AutoSkip = commandContext.Args[0].ToLower() == "on" ||
							commandContext.Args[0].ToLower() == "enable" ||
							commandContext.Args[0].ToLower() == "true" ||
							commandContext.Args[0].ToLower() == "yes";

			commandContext.Reply($"User {commandContext.Player.Name}'s autoskip has been {(user.AutoSkip ? "enabled" : "disabled")}");

			await userDb.SaveAsync();
		}

		[BotEvent(BotEventType.Command, "skip")]
		public async Task OnSkipCommand(CommandContext commandContext)
		{
			if (Data.Queue[0] == commandContext.Player?.Name)
				await SkipHost();
		}

		[BotEvent(BotEventType.Command, "queue")]
		public async Task OnDisplayQueueCommand(CommandContext commandContext)
		{
			commandContext.Reply(await GetQueueMessage());
		}

		[BotEvent(BotEventType.Command, "queueposition")]
		public void OnQueuePositionCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

			commandContext.Reply($"{commandContext.Player.Name}, you are position {Data.Queue.IndexOf(commandContext.Player.Name) + 1} in queue.");
		}

		[BotEvent(BotEventType.Command, "sethost")]
		public Task OnSetHostCommand(CommandContext commandContext)
		{
			string? user = Data.Queue.FirstOrDefault(x => x.ToIrcNameFormat() == commandContext.Args[0].ToIrcNameFormat());

			if (user == null)
			{
				commandContext.Reply($"Player '{commandContext.Args[0]}' could not be found.");
				return Task.CompletedTask;
			}

			Data.Queue.Remove(user);
			Data.Queue.Insert(0, user);
			EnsureHost();
			return Task.CompletedTask;
		}

		[BotEvent(BotEventType.Command, "setqueueposition")]
		public void OnSetQueuePositionCommand(CommandContext commandContext)
		{
			if (commandContext.Args.Length > 2)
			{
				commandContext.Reply($"Invalid argument amount, usage: {commandContext.Command.Usage}");
				return;
			}

			string? user = Data.Queue.FirstOrDefault(x => x.ToIrcNameFormat() == commandContext.Args[0].ToIrcNameFormat());

			if (user == null)
			{
				commandContext.Reply($"Player '{commandContext.Args[0]}' could not be found.");
				return;
			}

			if (!int.TryParse(commandContext.Args[1], out int result) || result < 1 || result > Data.Queue.Count)
			{
				commandContext.Reply($"Invalid queue position, usage: {commandContext.Command.Usage}");
				return;
			}

			Data.Queue.Remove(user);
			Data.Queue.Insert(result - 1, user);
			EnsureHost();
			return;
		}

		[BotEvent(BotEventType.Command, "forceskip")]
		public async Task OnForceSkipCommand(CommandContext commandContext)
		{
			await SkipHost();
		}

		#endregion

		#region Bot Events

		[BotEvent(BotEventType.Initialize)]
		public void Initialize()
		{
			Logger.Log(LogLevel.Info, "HostQueueBehavior: Initializing");
			Data.Queue = [];
		}

		[BotEvent(BotEventType.SettingsUpdated)] // Makes sure queue is valid after bot restart as players might have left during downtime
		public async Task OnSettingsUpdated()
		{
			await EnsureQueueValidity();
		}

		[BotEvent(BotEventType.PlayerJoined)]
		public async Task OnPlayerJoined(MultiplayerPlayer player)
		{
			Logger.Log(LogLevel.Trace, $"HostQueueBehavior: Player Joined {player.Name}");

			await using var userDb = new UserDb();

			await userDb.FindOrCreateUser(player.Name);

			Data.Queue.Add(player.Name);

			if (context.MultiplayerLobby.PlayerCount == 1)
				EnsureHost();
		}

		[BotEvent(BotEventType.PlayerDisconnected)]
		public Task OnPlayerDisconnected(MultiplayerPlayer player)
		{
			Logger.Log(LogLevel.Info, $"HostQueueBehavior: Player disconnected {player.Name}");

			Data.Queue.Remove(player.Name);

			if (Data.Queue.Count > 0)
				EnsureHost();
			else // Clear host so the lobby doesn't believe the host is already set if a single player rejoins lobby.
				context.SendMessage("!mp clearhost");

			return Task.CompletedTask;
		}

		[BotEvent(BotEventType.MatchFinished)]
		public async Task OnMatchFinished()
		{
			Logger.Log(LogLevel.Info, "HostQueueBehavior: Match Finished");

			await SkipHost();
			context.SendMessage(await GetQueueMessage());
		}


		[BotEvent(BotEventType.HostChanged)]
		public Task OnHostChanged()
		{
			EnsureHost();

			return Task.CompletedTask;
		}

		#endregion

		#region Queue Methods

		void EnsureHost()
		{
			if (Data.Queue.Count == 0)
				return;

			Logger.Log(LogLevel.Trace, "HostQueueBehavior: Ensuring Queue");

			// Host already set
			if (context?.MultiplayerLobby.Host != null && Data.Queue[0].ToIrcNameFormat() == context.MultiplayerLobby.Host.Name.ToIrcNameFormat())
				return;

			context?.SendMessage($"!mp host {context.GetPlayerIdentifier(Data.Queue[0])}");
		}

		void RotateQueue()
		{
			if (Data.Queue.Count == 0)
				return;

			Logger.Log(LogLevel.Trace, "HostQueueBehavior: Rotating Queue");

			string currentHost = Data.Queue[0];

			Data.Queue.RemoveAt(0);
			Data.Queue.Add(currentHost);
		}

		async Task SkipHost()
		{
			if (Data.Queue.Count == 0)
				return;

			Logger.Log(LogLevel.Trace, "HostQueueBehavior: Skipping to Next Host");

			await using var userDb = new UserDb();

			RotateQueue();

			foreach (string player in Data.Queue.ToList())
			{
				if ((await userDb.FindOrCreateUser(Data.Queue[0])).AutoSkip)
				{
					RotateQueue();
					continue;
				}
				break;
			}

			EnsureHost();
		}

		async Task<string> GetQueueMessage()
		{
			await using var userDb = new UserDb();

			List<string> names = [];

			for (int i = 0; i < Data.Queue.Count; i++)
			{
				string player = Data.Queue[i];

				var user = await userDb.FindOrCreateUser(player);

				// Add zero width space to all except host to avoid mentioning them.
				var finalName = i != 0 ? $"{player[0]}\u200B{player[1..]}" : player;

				if (user.AutoSkip)
					finalName += "*";

				names.Add(finalName);
			}

			return $"Queue: {string.Join(", ", names)}";
		}

		async Task EnsureQueueValidity()
		{
			// Remove players no longer in lobby
			foreach (var player in Data.Queue.ToList().Where(player => context.MultiplayerLobby.Players.All(x => x.Name.ToIrcNameFormat() != player.ToIrcNameFormat())))
			{
				Data.Queue.Remove(player);
			}

			// Add players in lobby who are not already in queue
			foreach (var player in context.MultiplayerLobby.Players.Where(player => !Data.Queue.Any(x => x.ToIrcNameFormat() == player.Name.ToIrcNameFormat())))
			{
				var userDb = new UserDb();
				var user = await userDb.FindOrCreateUser(player.Name);

				Data.Queue.Add(player.Name);
			}

			// Remove duplicates, shouldn't be a problem in theory, but had it happen a couple times
			Data.Queue = Data.Queue.Distinct().ToList();

			EnsureHost();
		}

		#endregion
	}
}
