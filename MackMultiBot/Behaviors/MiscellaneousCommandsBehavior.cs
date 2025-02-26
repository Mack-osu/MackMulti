using BanchoSharp.Multiplayer;
using Humanizer;
using Humanizer.Localisation;
using MackMulti.Database.Databases;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Data;
using MackMultiBot.Database.Entities;
using MackMultiBot.Extensions;
using MackMultiBot.Interfaces;
using MackMultiBot.OsuData.Extensions;
using OsuSharp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class MiscellaneousCommandsBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("MiscellaneousCommandsBehaviorLogger");

		readonly BehaviorDataProvider<MiscellaneousCommandsBehaviorData> _dataProvider = new(context.Lobby);
		private MiscellaneousCommandsBehaviorData Data => _dataProvider.Data;
		public async Task SaveData() => await _dataProvider.SaveData();

		#region Command Events

		[BotEvent(BotEventType.Command, "playtime")]
		public void OnPlaytimeCommand(CommandContext commandContext)
		{
			var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == commandContext.Player?.Name);
			var totalPlaytime = TimeSpan.FromSeconds(commandContext.User.Playtime);
			var currentPlaytime = TimeSpan.FromSeconds(0);

			if (record != null)
			{
				currentPlaytime = DateTime.UtcNow - record.JoinTime;
				totalPlaytime += currentPlaytime;
			}

			commandContext.Reply($"{commandContext.Player?.Name} has been here for {currentPlaytime.Humanize(3, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} " +
								$"({totalPlaytime.Humanize(4, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} ({totalPlaytime.TotalHours:F0}h) in total).");
		}

		[BotEvent(BotEventType.Command, "playcount")]
		public void OnPlaycountCommand(CommandContext commandContext)
		{
			commandContext.Reply($"{commandContext.Player?.Name} has played a total of {commandContext.User.Playcount} matches, with {commandContext.User.MatchWins} wins!");
		}

		[BotEvent(BotEventType.Command, "bestmapscore")]
		public async Task OnBestMapScoreCommand(CommandContext commandContext)
		{
			if (commandContext.Player?.Id == null)
				return;

			await using var scoreDb = new ScoreDb();
			var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
			BeatmapInformation mapInfo = mapManagerDataProvider.Data.BeatmapInfo;

			var bestScore = (await scoreDb.GetMapScoresOfUser(mapInfo.Id, commandContext.Player.Id.Value))?.MaxBy(x => x.TotalScore);

			commandContext.Reply(bestScore != null ?
				$"{commandContext.Player.Name}'s best score on [https://osu.ppy.sh/b/{mapInfo.Id} {mapInfo.Name}] is a {bestScore.GetAccuracy():0.00}% {bestScore.Rank} rank with {bestScore.MaxCombo}x combo, {bestScore.Count300}/{bestScore.Count100}/{bestScore.Count50}/{bestScore.CountMiss}."
				: $"{commandContext.Player.Name}, you have not played this map in this lobby yet.");

			// TODO: Add PP calculations, probably want to store these in the score database save
		}

		[BotEvent(BotEventType.Command, "recentscore")]
		public async Task OnRecentScoreCommand(CommandContext commandContext)
		{
			if (commandContext.Player?.Id == null)
				return;

			await using var scoreDb = new ScoreDb();

			var latestScore = (await scoreDb.GetScoresOfUser(commandContext.Player.Id.Value))?.FirstOrDefault();

			if (latestScore == null)
			{
				commandContext.Reply($"{commandContext.Player.Name}, you have do not have any scores in this lobby yet.");
				return;
			}

			var map = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync((int)latestScore.BeatmapId));

			if (map == null)
			{
				_logger.Warn("MiscellaneousCommandsBehavior: Could not find beatmap of id '{beatmapId}'", (int)latestScore.BeatmapId);
				return;
			}

			var set = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapSetAsync(map!.SetId));

			commandContext.Reply($"{commandContext.Player.Name}'s most recent score in this lobby is a {latestScore.GetAccuracy():0.00}% {latestScore.Rank} rank with {latestScore.MaxCombo}x combo, {latestScore.Count300}/{latestScore.Count100}/{latestScore.Count50}/{latestScore.CountMiss} on [https://osu.ppy.sh/b/{latestScore.BeatmapId} {set?.Title}].");

			// TODO: Add PP calculations, probably want to store these in the score database save
		}

		#endregion

		#region Bot Events

		[BotEvent(BotEventType.PlayerJoined)]
		public void OnPlayerJoined(MultiplayerPlayer player)
		{
			Data.PlayerTimeRecords.Add(new MiscellaneousCommandsBehaviorData.PlayerJoinRecord
			{
				PlayerName = player.Name,
				JoinTime = DateTime.UtcNow
			});
		}

		[BotEvent(BotEventType.PlayerDisconnected)]
		public async Task OnPlayerDisconnected(MultiplayerPlayer player)
		{
			await using var userDb = new UserDb();

			var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == player.Name);
			if (record == null)
				return;

			var user = await userDb.FindOrCreateUser(player.Name);

			user.Playtime += (int)(DateTime.UtcNow - record.JoinTime).TotalSeconds;

			await userDb.SaveAsync();

			Data.PlayerTimeRecords.Remove(record);
		}

		[BotEvent(BotEventType.MatchStarted)]
		public void OnMatchStarted()
		{
			var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);

			Data.LastPlayedBeatmapInfo = mapManagerDataProvider.Data.BeatmapInfo;
		}

		[BotEvent(BotEventType.MatchFinished)]
		public async Task OnMatchFinished()
		{
			await context.MultiplayerLobby.RefreshSettingsAsync();
			await Task.Delay(10000);

			await Task.Run(async () =>
			{
				var recentScores = await GetRecentScores();

				await StoreMapData(recentScores);
				await StorePlayerFinishData(recentScores);
				//await AnnounceLeaderboardResults(recentScores);
			});
		}

		#endregion

		#region Score Saving

		async Task<List<ScoreResult>> GetRecentScores()
		{
			var players = context.MultiplayerLobby.Players.Where(x => x.Id != null).ToList();
			var getScoreTasks = new List<Task<OsuSharp.Models.Scores.Score[]?>>();
			
			_logger.Info(string.Join(", ", players.Select(x => x.Name)));

			await context.Lobby.Bot.OsuApiClient.EnsureAccessTokenAsync();

			for (int i = 0; i < players.Count; i++)
			{
				int index = i;

				getScoreTasks.Add(Task.Run(async () =>
				{
					await Task.Delay(index * 250);

					return await context.UsingApiClient(async (apiClient) => await apiClient.GetUserScoresAsync(players[index].Id!.Value, UserScoreType.Recent, 1, 1, "osu", 1));
				}));
			}

			await Task.WhenAll(getScoreTasks);

			return players.Select(player => new ScoreResult((MultiplayerPlayer)player, getScoreTasks.Select(x => x.Result?.FirstOrDefault()).ToList().FirstOrDefault(x => x?.UserId == player.Id!))).ToList();
		}

		async Task StoreMapData(IReadOnlyList<ScoreResult> recentScores)
		{
			if (Data.LastPlayedBeatmapInfo == null)
			{
				return;
			}

			await using var matchDb = new MatchDb();

			var map = new PlayedMap
			{
				BeatmapId = Data.LastPlayedBeatmapInfo.Id,
				Time = DateTime.UtcNow,
			};

			await matchDb.AddAsync(map);
			await matchDb.SaveAsync();

			await StoreScoreData(recentScores, map);
		}

		async Task StoreScoreData(IReadOnlyList<ScoreResult> recentScores, PlayedMap map)
		{
			await using var userDb = new UserDb();
			await using var scoreDb = new ScoreDb();

			try
			{
				foreach (var result in recentScores)
				{
					var score = result.Score;
					var user = await userDb.FindOrCreateUser(result.Player.Name);

					await scoreDb.AddAsync(new Score
					{
						UserId = user.Id,
						PlayerId = result.Player.Id,
						LobbyId = context.Lobby.LobbyConfigurationId - 1,
						MapId = map.Id,
						OsuScoreId = score.Id,
						BeatmapId = score.Beatmap!.Id,
						TotalScore = score.TotalScore,
						Rank = score.IsPass ? score.Grade.GetOsuRank() : OsuRank.F,
						MaxCombo = score.MaxCombo,
						Count300 = score.Statistics.Count300,
						Count100 = score.Statistics.Count100,
						Count50 = score.Statistics.Count50,
						CountMiss = score.Statistics.Misses,
						Mods = score.GetModsBitset(),
						Time = DateTime.UtcNow
					});
				}

				_logger.Trace($"MiscellaneousCommandsBehavior: Stored {recentScores.Count} scores for game {map.Id}");
			}
			catch (Exception e)
			{
				_logger.Error($"MiscellaneousCommandsBehavior: Exception at StoreScoreData(): {e}");
			}

			await scoreDb.SaveAsync();
		}

		async Task StorePlayerFinishData(IReadOnlyList<ScoreResult> recentScores)
		{
			await using var userDb = new UserDb();

			var highestScorePlayer = recentScores.MaxBy(x => x.Player.Score);
			if (context.MultiplayerLobby.Players.Count >= 3 && highestScorePlayer is not null)
			{
				var user = await userDb.FindOrCreateUser(highestScorePlayer.Player.Name);

				user.MatchWins++;
			}

			foreach (var result in recentScores)
			{
				var user = await userDb.FindOrCreateUser(result.Player.Name);

				user.Playcount++;
			}

			await userDb.SaveAsync();
		}

		#endregion
	}
}
