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
			_logger.Trace($"MiscellaneousCommandsBehavior: Match Finished");
			// Wait 10 seconds to save recent scores data.
			await Task.Delay(10000);
			_logger.Trace($"MiscellaneousCommandsBehavior: Waited 10 seconds");

			await Task.Run(async () =>
			{
				_logger.Trace($"MiscellaneousCommandsBehavior: Inside Task.Run");
				var recentScores = await GetRecentScores();

				_logger.Trace($"MiscellaneousCommandsBehavior: Ran Recent scores");
				await StoreMapData(recentScores);
				_logger.Trace($"MiscellaneousCommandsBehavior: Ran storemapdata");
				await StorePlayerFinishData(recentScores);

				_logger.Trace($"MiscellaneousCommandsBehavior: Ran storeplayerfinishdata");
				//await AnnounceLeaderboardResults(recentScores);
			});
		}

		#endregion

		#region Score Saving

		async Task<List<ScoreResult>> GetRecentScores()
		{
			var players = context.MultiplayerLobby.Players.Where(x => x.Id != null && x.Score > 0).ToList();
			var getScoreTasks = new List<Task<OsuSharp.Models.Scores.Score[]?>>();

			await context.Lobby.Bot.OsuApiClient.EnsureAccessTokenAsync();

			for (int i = 0; i < players.Count; i++)
			{
				int index = i;

				getScoreTasks.Add(Task.Run(async () =>
				{
					await Task.Delay(index * 250);

					return await context.UsingApiClient(async (apiClient) => await apiClient.GetUserScoresAsync(players[index].Id!.Value, UserScoreType.Recent, true, true, Ruleset.Osu, 1));
				}));
			}

			await Task.WhenAll(getScoreTasks);
			_logger.Trace("MiscellaneousCommandsBehavior: Getting Recent scores: {getScoreTasks}", getScoreTasks.Count);

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
					if (result.Score?.Statistics.Count50 == null)
					{
						_logger.Trace($"MiscellaneousCommandsBehavior: CONTINUED");
						continue;
					}

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
						Rank = score.Grade.GetOsuRank(),
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
