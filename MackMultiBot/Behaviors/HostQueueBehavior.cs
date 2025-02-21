using BanchoSharp.Multiplayer;
using MackMulti.Database.Databases;
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

namespace MackMultiBot.Behaviors
{
	public class HostQueueBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		static NLog.Logger _logger = NLog.LogManager.GetLogger("HostQueueHandlerLogger");

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
			_logger.Info("HostQueueBehavior: Initializing");
			Data.Queue = new();
		}

		[BotEvent(BotEventType.PlayerJoined)]
		public async Task OnPlayerJoined(MultiplayerPlayer player)
		{
			_logger.Trace("HostQueueBehavior: Player Joined {player}", player.Name);

			await using var userDb = new UserDb();

			// Temporary yucky, just want it here during testing
			await using var dbContext = new BotDatabaseContext();
			string formattedUsername = player.Name.ToIrcNameFormat();
			if (dbContext.Users
				.AsEnumerable() // Pulls data into memory
				.FirstOrDefault(x => x.Name.ToIrcNameFormat() == formattedUsername) == null)
			{
				Console.WriteLine("New player \n\n");
				context.Lobby.BanchoConnection.MessageHandler.SendMessage(player.TargetableName(), "Welcome to the lobby!");
				context.Lobby.BanchoConnection.MessageHandler.SendMessage(player.TargetableName(), "This bot currently is under active development and may not have all the features you'd expect, if you encounter any issues, please let me know!");
			}

			var user = await userDb.FindOrCreateUser(player.Name);

			Data.Queue.Add(player.Name);

			if (Data.Queue.Count == 1)
				EnsureHost();
		}

		[BotEvent(BotEventType.PlayerDisconnected)]
		public Task OnPlayerDisconnected(MultiplayerPlayer player)
		{
			_logger.Trace("HostQueueBehavior: Player disconnected {player}", player.Name);

			Data.Queue.Remove(player.Name);

			if (Data.Queue.Count > 0)
				EnsureHost();

			return Task.CompletedTask;
		}

		[BotEvent(BotEventType.MatchFinished)]
		public async Task OnMatchFinished()
		{
			_logger.Trace("HostQueueBehavior: Match Finished");

			await SkipHost();
			context.SendMessage(await GetQueueMessage());
		}

		#endregion

		#region Queue Methods

		void EnsureHost()
		{
			if (Data.Queue.Count == 0)
				return;

			_logger.Trace("HostQueueBehavior: Ensuring Queue");

			// Host already set
			if (context.MultiplayerLobby.Host != null && Data.Queue[0].ToIrcNameFormat() == context.MultiplayerLobby.Host.Name.ToIrcNameFormat())
				return;

			context.SendMessage($"!mp host {context.GetPlayerIdentifier(Data.Queue[0])}");
		}

		void RotateQueue()
		{
			if (Data.Queue.Count == 0)
				return;

			_logger.Trace("HostQueueBehavior: Rotating Queue");

			string currentHost = Data.Queue[0];

			Data.Queue.RemoveAt(0);
			Data.Queue.Add(currentHost);
		}

		async Task SkipHost()
		{
			if (Data.Queue.Count == 0)
				return;

			_logger.Trace("HostQueueBehavior: Skipping to Next Host");

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
			_logger.Trace("HostQueueBehavior: Getting Queue Message");

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

		#endregion
	}
}
