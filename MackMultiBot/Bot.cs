using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Bancho;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using Microsoft.EntityFrameworkCore;
using OsuSharp;

namespace MackMultiBot
{
	public class Bot(BotConfiguration botConfiguration)
	{
		public ILobby? Lobby { get; private set; }
		public BanchoConnection BanchoConnection { get; } = new(botConfiguration);
		public OsuApiClient OsuApiClient { get; } = new(botConfiguration.ApiClientId, botConfiguration.ApiClientSecret);

		public CommandProcessor? CommandProcessor { get; private set; }

		public Action<IChatChannel>? OnBanchoChannelJoined;
		public Action<string>? OnBanchoChannelJoinFailed;
		public Action<IMultiplayerLobby>? OnBanchoLobbyCreated;

		// This class will also be used to handle creating and starting lobbies.

		public async Task StartAsync()
		{
			Logger.Log(LogLevel.Trace, "Bot starting");

			BanchoConnection.OnReady += OnBanchoReady;

			LoadLobbyConfiguration();

			await BanchoConnection.StartAsync();
		}

		void LoadLobbyConfiguration()
		{
			Logger.Log(LogLevel.Trace, "Bot: Loading lobby configuration.");

			Lobby = new Lobby(this, new LobbyConfiguration()
			{
				Name = botConfiguration.LobbyName,
				Identifier = botConfiguration.LobbyIdentifier,
				Mode = (GameMode)botConfiguration.Mode,
				TeamMode = (LobbyFormat)botConfiguration.TeamMode,
				ScoreMode = (WinCondition)botConfiguration.ScoreMode,
				Mods = botConfiguration.Mods,
				Size = botConfiguration.Size,
				Password = botConfiguration.Password,
				RuleConfig = new()
				{
					LimitDifficulty = botConfiguration.LimitDifficulty,
					LimitMapLength = botConfiguration.LimitMapLength,
					MinimumDifficulty = botConfiguration.MinimumDifficulty,
					MaximumDifficulty = botConfiguration.MaximumDifficulty,
					DifficultyMargin = botConfiguration.DifficultyMargin,
					MinimumMapLength = botConfiguration.MinimumMapLength,
					MaximumMapLength = botConfiguration.MaximumMapLength
				}
			});

			Logger.Log(LogLevel.Info, $"Bot: Loaded lobby configuration for lobby '{botConfiguration.LobbyName}'");
		}

		private async void OnBanchoReady()
		{
			if (BanchoConnection.BanchoClient == null)
			{
				Logger.Log(LogLevel.Error, "Bot: BanchoClient null when ready? how+????");
				return;
			}

			if (Lobby == null)
			{
				Logger.Log(LogLevel.Error, "Bot: No lobby found.");
				return;
			}

			BanchoConnection.BanchoClient.OnChannelJoined += OnBanchoChannelJoined;
			BanchoConnection.BanchoClient.OnChannelJoinFailure += OnBanchoChannelJoinFailed;
			BanchoConnection.BanchoClient.BanchoBotEvents.OnTournamentLobbyCreated += OnBanchoLobbyCreated;

            await Lobby.ConnectOrCreateAsync();

			if (CommandProcessor == null)
			{
				CommandProcessor = new(this);
				CommandProcessor.Start();
			}
		}
	}
}
