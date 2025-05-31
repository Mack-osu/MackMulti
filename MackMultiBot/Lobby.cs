using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Bancho;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using MackMultiBot.Logging;
using MackMultiBot.Services;

namespace MackMultiBot
{
	public class Lobby : ILobby
	{
		public Bot Bot { get; init; }

		public BanchoConnection BanchoConnection { get; init; }

		public MultiplayerLobby? MultiplayerLobby { get; private set; } = null;
		public BehaviorEventProcessor? BehaviorEventProcessor { get; private set; }

		Messager? _messager;

		public LobbyConfiguration LobbyConfiguration { get; private set; }

		public ITimerHandler? TimerHandler { get; private set; }
		public IVoteHandler? VoteHandler { get; private set; }

		public string ChannelId { get; private set; } = string.Empty;

		bool _shouldCreateNewInstance;

		public Lobby(Bot bot, LobbyConfiguration lobbyConfig)
		{
			Bot = bot;
			BanchoConnection = bot.BanchoConnection;

			Bot.OnBanchoChannelJoined += OnChannelJoined;
			Bot.OnBanchoChannelJoinFailed += OnChannelJoinedFailure;
			Bot.OnBanchoLobbyCreated += OnLobbyCreated;

			LobbyConfiguration = lobbyConfig;
		}

		public async Task ConnectOrCreateAsync(bool IsRecreation = false)
		{
			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Error, "Lobby: BanchoClient not initialized when creating lobby");
				throw new InvalidOperationException("BanchoClient not initialized when creating lobby");
			}

			if (MultiplayerLobby != null)
			{
				Logger.Log(LogLevel.Trace, $"Lobby: Lobby instance already exists, parting from previous instance {MultiplayerLobby.ChannelName}");

				await BanchoConnection.BanchoClient!.PartChannelAsync(MultiplayerLobby.ChannelName);
				await ShutdownInstance();
			}

			var previousInstance = await GetRecentRoomInstance();
			var existingChannel = string.Empty;

			// If we have a previous instance, attempt to join via that channel instead.
			if (previousInstance != null && !IsRecreation)
				existingChannel = previousInstance.Channel;

			ChannelId = existingChannel;
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

			ChannelId = lobby.ChannelName;
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
			if (channel.ChannelName != ChannelId)
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

				context.LobbyBehaviorData.RemoveRange(context.LobbyBehaviorData);

				context.LobbyInstances.Remove(previousInstance);

				await context.SaveChangesAsync();
			}

			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Warn, "Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
				return;
			}

			// Not the channel we were trying to join, ignore
			if (attemptedChannel != ChannelId)
			{
				Logger.Log(LogLevel.Trace, $"Lobby: Ignoring channel {attemptedChannel}, not the channel we were trying to join, ({ChannelId}).");
				return;
			}

			Logger.Log(LogLevel.Warn, $"Lobby: Failed to join channel {attemptedChannel}, creating new instance");

			_shouldCreateNewInstance = true;

			await BanchoConnection.BanchoClient.MakeTournamentLobbyAsync(LobbyConfiguration.Name)!;
		}

		#endregion

		async Task ConstructInstance()
		{
			// Initialize behaviors
			BehaviorEventProcessor = new(this);
			TimerHandler = new TimerHandler(this);
			VoteHandler = new VoteHandler(this);

			BehaviorEventProcessor.RegisterBehavior("TestBehavior");
			BehaviorEventProcessor.RegisterBehavior("HostQueueBehavior");
			BehaviorEventProcessor.RegisterBehavior("LobbyManagerBehavior");
			BehaviorEventProcessor.RegisterBehavior("StartBehavior");
			BehaviorEventProcessor.RegisterBehavior("MapManagerBehavior");
			BehaviorEventProcessor.RegisterBehavior("MiscellaneousCommandsBehavior");
			BehaviorEventProcessor.RegisterBehavior("LobbyWatchBehavior");

			BehaviorEventProcessor.Start();
			
			// Ensure database entry
			var recentRoomInstance = await GetRecentRoomInstance();
			if (recentRoomInstance == null)
			{
				await using var context = new BotDatabaseContext();

				context.LobbyInstances.Add(new LobbyInstance()
				{
					Channel = ChannelId
				});

				await context.SaveChangesAsync();
			}

			await TimerHandler.Start();
			await BehaviorEventProcessor.OnInitializeEvent();

			_messager = new(BanchoConnection.MessageHandler, ChannelId);
			BanchoConnection.MessageHandler.ChannelId = ChannelId;
			_messager.Start();

			Logger.Log(LogLevel.Trace, "Lobby: Lobby instance built successfully");
		}

		async Task<LobbyInstance?> GetRecentRoomInstance(string? channelId = null)
		{
			await using var context = new BotDatabaseContext();

			if (channelId != null)
				return await context.LobbyInstances.FirstOrDefaultAsync(x => x.Channel == channelId);

			return await context.LobbyInstances.FirstOrDefaultAsync();
		}

		public async void RemoveInstance()
		{
			var instance = await GetRecentRoomInstance(ChannelId);

			// Remove the lobby instance we failed to join from database.
			if (instance != null)
			{
				await using var context = new BotDatabaseContext();

				context.LobbyBehaviorData.RemoveRange(context.LobbyBehaviorData);

				context.LobbyInstances.Remove(instance);

				await context.SaveChangesAsync();
			}
		}

		async Task ShutdownInstance()
		{
			if (TimerHandler != null)
			{
				await TimerHandler.Stop();
				TimerHandler = null;
			}

			VoteHandler = null;

			BehaviorEventProcessor?.Stop();
			BehaviorEventProcessor = null;

			Logger.Log(LogLevel.Info, "Lobby: Lobby instance shut down successfully.");
		}
	}
}
