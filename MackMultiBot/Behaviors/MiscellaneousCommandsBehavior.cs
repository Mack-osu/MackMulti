using BanchoSharp.Multiplayer;
using Humanizer;
using Humanizer.Localisation;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Data;
using MackMultiBot.Database.Databases;
using MackMultiBot.Database.Entities;
using MackMultiBot.Extensions;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using MackMultiBot.OsuData.Extensions;
using OsuSharp.Enums;
using OsuSharp.Models.Scores;
using OsuSharp.Models.Users;
using System.Globalization;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class MiscellaneousCommandsBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		readonly BehaviorDataProvider<MiscellaneousCommandsBehaviorData> _dataProvider = new(context.Lobby);
		private MiscellaneousCommandsBehaviorData Data => _dataProvider.Data;
		public async Task SaveData() => await _dataProvider.SaveData();

		#region Command Events

		[BotEvent(BotEventType.Command, "playtime")]
		public async void OnPlaytimeCommand(CommandContext commandContext)
		{
			var userDb = new UserDb();

			// external player
			if (commandContext.Args.Length > 0)
			{
				var user = userDb.FindUser(commandContext.Args[0]);

				if (user == null)
				{
					commandContext.Reply($"User '{commandContext.Args[0]}' not found.");
					return;
				}

				var playTime = TimeSpan.FromSeconds(user.Playtime);

				commandContext.Reply($"{user.Name} has played for {playTime.Humanize(4, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)}. They are ranked #{await userDb.GetUserPlaytimeSpot(user.Name)} in total playtime.");
				return;
			}

			var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == commandContext.Player?.Name);
			var totalPlaytime = TimeSpan.FromSeconds(commandContext.User.Playtime);
			var currentPlaytime = TimeSpan.FromSeconds(0);

			if (record != null)
			{
				currentPlaytime = DateTime.UtcNow - record.JoinTime;
				totalPlaytime += currentPlaytime;
			}

			commandContext.Reply($"{commandContext.Player?.Name} has been here for {currentPlaytime.Humanize(3, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} " +
								$"({totalPlaytime.Humanize(4, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} ({totalPlaytime.TotalHours:F0}h) in total). " +
								$"They are ranked #{await userDb.GetUserPlaytimeSpot(commandContext.Player!.Name)} in total playtime.");
		}

		[BotEvent(BotEventType.Command, "playtimetop")]
		public async Task OnPlaytimeTopCommand(CommandContext commandContext)
		{
			var userDb = new UserDb();

			// Perhaps an optional parameter to get user at a certain spot?

			var playTimeTop4 = await userDb.GetTopUsersByPlayTime(4);

			commandContext.Reply($"#1: {playTimeTop4[1].Name} with {TimeSpan.FromSeconds(playTimeTop4[1].Playtime).Humanize(2, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} | " +
								$"#2: {playTimeTop4[2].Name} with {TimeSpan.FromSeconds(playTimeTop4[2].Playtime).Humanize(2, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} | " +
								$"#3: {playTimeTop4[3].Name} with {TimeSpan.FromSeconds(playTimeTop4[3].Playtime).Humanize(2, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)}");

			Logger.Log(LogLevel.Error, "EVEN MORE");
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

			var bestScore = (await scoreDb.GetMapScoresOfUserAsync(mapInfo.Id, commandContext.Player.Id.Value))?.MaxBy(x => x.TotalScore);

			commandContext.Reply(bestScore != null ?
				$"{commandContext.Player.Name}'s best score on [https://osu.ppy.sh/b/{mapInfo.Id} {mapInfo.Name}] is a " +
				$"{bestScore.GetAccuracy():0.00}% {bestScore.Rank} rank with {bestScore.MaxCombo}x combo, " +
				$"{bestScore.Count300}/{bestScore.Count100}/{bestScore.Count50}/{bestScore.CountMiss} played " +
				$"{bestScore.Time.Humanize(utcDate: true, culture: CultureInfo.InvariantCulture)}."
				: $"{commandContext.Player.Name}, you have not played this map in this lobby yet.");

			// TODO: Add PP calculations, probably want to store these in the score database save
		}

		[BotEvent(BotEventType.Command, "mapstats")]
		public async Task OnMapStatsCommand(CommandContext commandContext)
		{
			// [MapName] has been picked _ times. [User] holds the top score and it was last played _ ago

			await using var scoreDb = new ScoreDb();
			await using var matchDb = new MatchDb();
			await using var userDb = new UserDb();
			var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
			BeatmapInformation mapInfo = mapManagerDataProvider.Data.BeatmapInfo;

			var pickCount = await matchDb.GetMatchCountByMapIdAsync(mapInfo.Id);
			var lastScore = await matchDb.GetLastPlayedByMapIdAsync(mapInfo.Id);
			var bestScore = await scoreDb.GetBestMapScoreAsync(mapInfo.Id);

			string finalMessage = $"[https://osu.ppy.sh/b/{mapInfo.Id} {mapInfo.Name}] has been picked {pickCount} times";


			if (lastScore != null)
				finalMessage += $" and it was last played {lastScore.Time.Humanize(utcDate: true, culture: CultureInfo.InvariantCulture)}.";

			if (bestScore != null)
			{
				var bestScoreUser = userDb.GetUserFromDbIndex(bestScore.UserId);

				if (bestScoreUser != null)
				{
					finalMessage += $" [https://osu.ppy.sh/users/@{bestScoreUser.Name.ToIrcNameFormat()} {bestScoreUser.Name}] holds this lobby's top score with a " +
						$"{bestScore.GetAccuracy():0.00}% {bestScore.Rank} rank with {bestScore.MaxCombo}x combo, " +
						$"{bestScore.Count300}/{bestScore.Count100}/{bestScore.Count50}/{bestScore.CountMiss}";
				}
			}

			commandContext.Reply(finalMessage);
		}

		[BotEvent(BotEventType.Command, "recentscore")]
		public async Task OnRecentScoreCommand(CommandContext commandContext)
		{
			if (commandContext.Player?.Id == null)
				return;

			await using var scoreDb = new ScoreDb();

			var latestScore = (await scoreDb.GetScoresOfUserAsync(commandContext.Player.Id.Value))?.FirstOrDefault();

			if (latestScore == null)
			{
				commandContext.Reply($"{commandContext.Player.Name}, you have do not have any scores in this lobby yet.");
				return;
			}

			var map = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync((int)latestScore.BeatmapId));

			if (map == null)
			{
				Logger.Log(LogLevel.Warn, $"MiscellaneousCommandsBehavior: Could not find beatmap of id '{(int)latestScore.BeatmapId}'");
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
			
			Logger.Log(LogLevel.Trace, $"Getting recent scores of players: {string.Join(", ", players.Select(x => x.Name))}");

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

					if (score == null)
						continue;

					var user = await userDb.FindOrCreateUser(result.Player.Name);

					await scoreDb.AddAsync(new Database.Entities.Score
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

				Logger.Log(LogLevel.Trace, $"MiscellaneousCommandsBehavior: Stored {recentScores.Count} scores for game {map.Id}");
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, $"MiscellaneousCommandsBehavior: Exception at StoreScoreData(): {e}");
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
