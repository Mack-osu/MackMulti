using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Bancho;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public class Lobby : ILobby
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("LobbyLogger");

		public Bot Bot { get; init; }

		public BanchoConnection BanchoConnection { get; init; }

		public MultiplayerLobby? MultiplayerLobby { get; private set; } = null;
		public BehaviorEventProcessor BehaviorEventProcessor { get; private set; }

		public int LobbyConfigurationId { get; set; }


		private string _channelId = string.Empty;

		private bool _isCreatingInstance;

		public Lobby(Bot bot, int lobbyConfigurationId)
		{
			Bot = bot;
			BanchoConnection = bot.BanchoConnection;

			Bot.OnBanchoChannelJoined += OnChannelJoined;
			Bot.OnBanchoChannelJoinFailed += OnChannelJoinedFailure;
			Bot.OnBanchoLobbyCreated += OnLobbyCreated;

			LobbyConfigurationId = lobbyConfigurationId;
		}

		public async Task ConnectOrCreateAsync()
		{
			if (BanchoConnection.BanchoClient == null)
			{
				_logger.Error("Lobby: BanchoClient not initialized when creating lobby");
				throw new InvalidOperationException("BanchoClient not initialized when creating lobby");
			}

			if (MultiplayerLobby != null)
			{
				_logger.Trace("Lobby: Lobby instance already exists, parting from previous instance...");
				await BanchoConnection.BanchoClient!.PartChannelAsync(MultiplayerLobby.ChannelName); 
			}

			var lobbyConfiguration = await GetLobbyConfiguration();
			var previousInstance = await GetRecentRoomInstance();
			var existingChannel = string.Empty;

			// If we have a previous instance, attempt to join via that channel instead.
			if (previousInstance != null)
				existingChannel = previousInstance.Channel;

			_channelId = existingChannel;
			_isCreatingInstance = existingChannel.Length == 0;

			if (!_isCreatingInstance)
			{
				_logger.Trace("Lobby: Attempting to join existing channel '{ExistingChannel}' for lobby '{LobbyName}'...", existingChannel, lobbyConfiguration.Name);

				//await BanchoConnection.BanchoClient.PartChannelAsync(existingChannel);
				//await Task.Delay(10000);
				await BanchoConnection.BanchoClient.JoinChannelAsync(existingChannel);

				Console.WriteLine(existingChannel);
			}
			else
			{
				_logger.Trace("Lobby: Creating new channel for lobby '{LobbyName}'", lobbyConfiguration.Name);

				await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(lobbyConfiguration.Name)!;
			}
		}

		#region Channel Join and Lobby Created

		public async void OnLobbyCreated(IMultiplayerLobby lobby)
		{
			if (BanchoConnection.BanchoClient == null)
			{
				_logger.Warn("Lobby: BanchoConnection.BanchoClient is null during lobby creation event");
				return;
			}

			if (!_isCreatingInstance)
				return;

			_channelId = lobby.ChannelName;
			_isCreatingInstance = false;

			MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(lobby.ChannelName[4..]), lobby.ChannelName);

			await ConstructInstance();
		}

		public async void OnChannelJoined(IChatChannel channel)
		{
			if (BanchoConnection.BanchoClient == null)
			{
				_logger.Warn("Lobby: BanchoConnection.BanchoClient is null during channel join.");
				return;
			}

			// Not the channel we were trying to join, ignore
			if (channel.ChannelName != _channelId)
				return;

			// We will be waiting for the lobby creation event instead
			if (_isCreatingInstance)
				return;

			_logger.Trace("Lobby: Joined channel {Channel} successfully, building lobby instance...", channel.ChannelName);

			MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(channel.ChannelName[4..]), channel.ChannelName);

			await ConstructInstance();
		}

		public async void OnChannelJoinedFailure(string attemptedChannel)
		{
			if (BanchoConnection.BanchoClient == null)
			{
				_logger.Warn("Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
				return;
			}

			// Not the channel we were trying to join, ignore
			if (attemptedChannel != _channelId)
			{
				_logger.Trace("Lobby: Ignoring channel {AttemptedChannel}, not the channel we were trying to join, ({ChannelId}).", attemptedChannel, _channelId);
				return;
			}

			var lobbyConfiguration = await GetLobbyConfiguration();

			_logger.Warn("Lobby: Failed to join channel {attemptedChannel}, creating new isntance", attemptedChannel);

			_isCreatingInstance = true;

			await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(lobbyConfiguration.Name);
		}

		#endregion

		async Task ConstructInstance()
		{
			var lobbyConfiguration = GetLobbyConfiguration();

			// Initialize behaviors
			BehaviorEventProcessor = new(this);

			BehaviorEventProcessor.RegisterBehavior("TestBehavior");
			BehaviorEventProcessor.RegisterBehavior("HostQueueBehavior");

			BehaviorEventProcessor.Start();

			// Ensure database entry
			var recentRoomInstance = await GetRecentRoomInstance();
			if (recentRoomInstance == null)
			{
				await using var context = new BotDatabaseContext();

				context.LobbyInstances.Add(new LobbyInstance()
				{
					Channel = _channelId,
					LobbyConfigurationId = LobbyConfigurationId
				});

				await context.SaveChangesAsync();
			}

			await BehaviorEventProcessor.OnInitializeEvent();

			// Temporary thing, will make lobby configs somewhere else in the future.
			BanchoConnection.MessageHandler.SendMessage(_channelId, "!mp password " + lobbyConfiguration.Result.Password); 

			_logger.Trace("Lobby: Lobby instance built successfully");
		}

		public async Task<LobbyConfiguration> GetLobbyConfiguration()
		{
			await using var context = new BotDatabaseContext();

			var configuration = await context.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == LobbyConfigurationId);
			if (configuration == null)
			{
				_logger.Error("Lobby: Failed to find lobby configuration.");

				throw new InvalidOperationException("Failed to find lobby configuration.");
			}

			return configuration;
		}

		async Task<LobbyInstance?> GetRecentRoomInstance(string? channelId = null)
		{
			await using var context = new BotDatabaseContext();

			var query = context.LobbyInstances
				.OrderByDescending(x => x.Id)
				.Where(x => x.LobbyConfigurationId == LobbyConfigurationId);

			if (channelId != null)
				query = query.Where(x => x.Channel == channelId);

			return await query.FirstOrDefaultAsync();
		}
	}
}
