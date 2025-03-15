using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Bancho;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MackMultiBot.Logging;
using MackMultiBot.Bancho.Data;

namespace MackMultiBot
{
	public class Lobby : ILobby
	{
		public Bot Bot { get; init; }

		public BanchoConnection BanchoConnection { get; init; }

		public MultiplayerLobby? MultiplayerLobby { get; private set; } = null;
		public BehaviorEventProcessor? BehaviorEventProcessor { get; private set; }

		Messager? _messager;

		public string LobbyIdentifier { get; set; }
		public LobbyConfiguration LobbyConfiguration { get; private set; }

		public ITimerHandler? TimerHandler { get; private set; }

		private string _channelId = string.Empty;

		private bool _shouldCreateNewInstance;

		public Lobby(Bot bot, LobbyConfiguration lobbyConfig)
		{
			Bot = bot;
			BanchoConnection = bot.BanchoConnection;

			Bot.OnBanchoChannelJoined += OnChannelJoined;
			Bot.OnBanchoChannelJoinFailed += OnChannelJoinedFailure;
			Bot.OnBanchoLobbyCreated += OnLobbyCreated;

			LobbyConfiguration = lobbyConfig;
			LobbyIdentifier = lobbyConfig.Identifier;
		}

		public async Task ConnectOrCreateAsync()
		{
			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Error, "Lobby: BanchoClient not initialized when creating lobby");
				throw new InvalidOperationException("BanchoClient not initialized when creating lobby");
			}

			if (MultiplayerLobby != null)
			{
				Logger.Log(LogLevel.Trace, "Lobby: Lobby instance already exists, parting from previous instance...");
				await BanchoConnection.BanchoClient!.PartChannelAsync(MultiplayerLobby.ChannelName);
			}

			var previousInstance = await GetRecentRoomInstance();
			var existingChannel = string.Empty;

			// If we have a previous instance, attempt to join via that channel instead.
			if (previousInstance != null)
				existingChannel = previousInstance.Channel;

			_channelId = existingChannel;
			_shouldCreateNewInstance = existingChannel.Length == 0;

			if (!_shouldCreateNewInstance)
			{
				Logger.Log(LogLevel.Trace, $"Lobby: Attempting to join existing channel '{existingChannel}' for lobby '{LobbyConfiguration.Name}'...");

				await BanchoConnection.BanchoClient.JoinChannelAsync(existingChannel);
			}
			else
			{
				Logger.Log(LogLevel.Trace, $"Lobby: Creating new channel for lobby '{LobbyConfiguration.Name}'");

				await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(LobbyConfiguration.Name)!;
			}
		}

		#region Channel Join and Lobby Created

		public async void OnLobbyCreated(IMultiplayerLobby lobby)
		{
			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Warn, "Lobby: BanchoConnection.BanchoClient is null during lobby creation event");
				return;
			}

			if (!_shouldCreateNewInstance)
				return;

			_channelId = lobby.ChannelName;
			_shouldCreateNewInstance = false;

			MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(lobby.ChannelName[4..]), lobby.ChannelName);

			var managerDataProvider = new BehaviorDataProvider<LobbyManagerBehaviorData>(this);
			managerDataProvider.Data.IsFreshInstance = true;
			await managerDataProvider.SaveData();

			await ConstructInstance();
		}

		public async void OnChannelJoined(IChatChannel channel)
		{
			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Warn, "Lobby: BanchoConnection.BanchoClient is null during channel join.");
				return;
			}

			// Not the channel we were trying to join, ignore
			if (channel.ChannelName != _channelId)
				return;

			// We will be waiting for the lobby creation event instead
			if (_shouldCreateNewInstance)
				return;

			Logger.Log(LogLevel.Trace, $"Lobby: Joined channel {channel.ChannelName} successfully, building lobby instance...");

			MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(channel.ChannelName[4..]), LobbyConfiguration.Name);
			await MultiplayerLobby.RefreshSettingsAsync();

			await ConstructInstance();
		}

		public async void OnChannelJoinedFailure(string attemptedChannel)
		{
			var previousInstance = await GetRecentRoomInstance(attemptedChannel);

			// Remove the lobby instance we failed to join from database.
			if (previousInstance != null)
			{
				await using var context = new BotDatabaseContext();

				context.LobbyBehaviorData.RemoveRange(context.LobbyBehaviorData.Where(x => x.LobbyIdentifier == previousInstance.Identifier));

				context.LobbyInstances.Remove(previousInstance);

				await context.SaveChangesAsync();
			}

			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Warn, "Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
				return;
			}

			// Not the channel we were trying to join, ignore
			if (attemptedChannel != _channelId)
			{
				Logger.Log(LogLevel.Trace, $"Lobby: Ignoring channel {attemptedChannel}, not the channel we were trying to join, ({_channelId}).");
				return;
			}

			Logger.Log(LogLevel.Warn, $"Lobby: Failed to join channel {attemptedChannel}, creating new isntance");

			_shouldCreateNewInstance = true;

			await BanchoConnection.BanchoClient.MakeTournamentLobbyAsync(LobbyConfiguration.Name)!;
		}

		#endregion

		async Task ConstructInstance()
		{
			// Initialize behaviors
			BehaviorEventProcessor = new(this);
			TimerHandler = new TimerHandler(this);

			BehaviorEventProcessor.RegisterBehavior("TestBehavior");
			BehaviorEventProcessor.RegisterBehavior("HostQueueBehavior");
			BehaviorEventProcessor.RegisterBehavior("LobbyManagerBehavior");
			BehaviorEventProcessor.RegisterBehavior("StartBehavior");
			BehaviorEventProcessor.RegisterBehavior("MapManagerBehavior");
			BehaviorEventProcessor.RegisterBehavior("MiscellaneousCommandsBehavior");

			BehaviorEventProcessor.Start();
			                                                             
			// Ensure database entry
			var recentRoomInstance = await GetRecentRoomInstance();
			if (recentRoomInstance == null)
			{
				await using var context = new BotDatabaseContext();

				context.LobbyInstances.Add(new LobbyInstance()
				{
					Channel = _channelId,
					Identifier = LobbyIdentifier
				});

				await context.SaveChangesAsync();
			}

			await TimerHandler.Start();
			await BehaviorEventProcessor.OnInitializeEvent();
			_messager = new(BanchoConnection.MessageHandler, _channelId);
			_messager.Start();

			Logger.Log(LogLevel.Trace, "Lobby: Lobby instance built successfully");
		}

		async Task<LobbyInstance?> GetRecentRoomInstance(string? channelId = null)
		{
			await using var context = new BotDatabaseContext();
			var query = context.LobbyInstances
				.OrderByDescending(x => x.Id)
				.Where(x => x.Identifier == LobbyIdentifier);

			if (channelId != null)
				query = query.Where(x => x.Channel == channelId);

			return await query.FirstOrDefaultAsync();
		}
	}
}
