using BanchoSharp;
using BanchoSharp.Interfaces;
using MackMultiBot.Bancho;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Database;
using MackMultiBot.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MackMultiBot
{
	public class Bot(BanchoClientConfiguration clientConfiguration)
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("BotLogger");

		public List<ILobby> Lobbies { get; } = [];

		public BanchoConnection BanchoConnection { get; } = new(clientConfiguration);

		public CommandProcessor CommandProcessor { get; private set; }

		public Action<IChatChannel> OnBanchoChannelJoined;
		public Action<string> OnBanchoChannelJoinFailed;
		public Action<IMultiplayerLobby> OnBanchoLobbyCreated;

		// This class will also be used to handle creating and starting lobbies.

		public async Task StartAsync()
		{
			_logger.Info("Bot starting");

			BanchoConnection.OnReady += OnBanchoReady;

			await LoadLobbiesFromDatabase();
			await BanchoConnection.StartAsync();
		}

		private async Task LoadLobbiesFromDatabase()
		{
			await using var context = new BotDatabaseContext();

			_logger.Trace("Bot: Loading lobby configurations...");

			var lobbyConfigurations = await context.LobbyConfigurations.ToListAsync();

			foreach (var lobbyConfiguration in lobbyConfigurations.Where(lobbyConfiguration => !Lobbies.Any(x => x.LobbyConfigurationId == lobbyConfiguration.Id)))
			{
				var lobby = new Lobby(this, lobbyConfiguration.Id);

				Lobbies.Add(lobby);

				//OnLobbyCreated?.Invoke(lobby);

				_logger.Info("Bot: Loaded lobby configuration with id {LobbyConfigurationId}", lobbyConfiguration.Id);
			}
		}

		private async void OnBanchoReady()
		{
			if (BanchoConnection.BanchoClient == null)
			{
				_logger.Error("Bot: BanchoClient null when ready? how+????");
				return;
			}

			BanchoConnection.BanchoClient.OnChannelJoined += OnBanchoChannelJoined;
			BanchoConnection.BanchoClient.OnChannelJoinFailure += OnBanchoChannelJoinFailed;
			BanchoConnection.BanchoClient.BanchoBotEvents.OnTournamentLobbyCreated += OnBanchoLobbyCreated;

			foreach (var lobby in Lobbies)
            {
                _logger.Info("Bot: Starting lobby with id {LobbyConfigurationId}...", lobby.LobbyConfigurationId);
                
                await lobby.ConnectOrCreateAsync();
            }

			CommandProcessor = new(this);
			CommandProcessor.Start();
		}
	}
}
