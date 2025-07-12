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
using OsuSharp.Models.Beatmaps;
using OsuSharp.Models.Scores;
using OsuSharp.Models.Users;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class MiscellaneousCommandsBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		readonly BehaviorDataProvider<MiscellaneousCommandsBehaviorData> _dataProvider = new(context.Lobby);
		private MiscellaneousCommandsBehaviorData Data => _dataProvider.Data;
		public async Task SaveData() => await _dataProvider.SaveData();

		#region Command Events

			#region Player Stats

		[BotEvent(BotEventType.Command, "playtime")]
		public async void OnPlaytimeCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

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

				commandContext.Reply($"{user.Name} has played for {playTime.Humanize(4, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} (#{await userDb.GetUserPlaytimeSpot(user.Name) + 1}).");
				return;
			}

			var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == commandContext.Player?.Name);
			var totalPlaytime = TimeSpan.FromSeconds(commandContext.User.Playtime);
			var currentPlaytime = TimeSpan.FromSeconds(0);

			if (record != null)
			{
				currentPlaytime = DateTime.UtcNow - record.InitialJoinTime;
				totalPlaytime += DateTime.UtcNow - record.TrackingStartTime;
			}

			string reply = $"{commandContext.Player?.Name} has been here for {currentPlaytime.Humanize(3, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)}" +
						   $", ({totalPlaytime.Humanize(4, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} in total (#{await userDb.GetUserPlaytimeSpot(commandContext.Player!.Name) + 1})).";

			if (currentPlaytime.TotalHours >= 10)
				reply += "Why on gods green earth are you still here?!? Go to bed !!";
			else if (currentPlaytime.TotalHours >= 6)
				reply += "6 hours... Don't you have anything better to do?";
			else if (currentPlaytime.TotalHours >= 5)
				reply += "5 hours in one go? that's a bit excessive";
			else if (currentPlaytime.TotalHours >= 4)
				reply += "Maybe you should take a break?";
			

				commandContext.Reply(reply);
		}

		[BotEvent(BotEventType.Command, "playcount")]
		public async Task OnPlaycountCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

			var userDb = new UserDb();

			int? playcountRanking = await userDb.GetUserPlaycountSpot(commandContext.Player.Name);
			int? matchWinsRanking = await userDb.GetUserMatchWinsSpot(commandContext.Player.Name);

			commandContext.Reply($"{commandContext.Player?.Name} has played a total of {commandContext.User.Playcount} matches (#{(playcountRanking + 1).ToString() ?? "unknown"}), " +
				$"with {commandContext.User.MatchWins} wins ((#{(matchWinsRanking + 1).ToString() ?? "unknown"})).");
		}

		[BotEvent(BotEventType.Command, "playtimetop")]
		public async Task OnPlaytimeTopCommand(CommandContext commandContext)
		{
			var userDb = new UserDb();

			// Perhaps an optional parameter to get user at a certain spot?

			var playTimeTop3 = await userDb.GetTopUsersByPlayTime(3);

			Dictionary<string, int> users = [];

			for (int i = 0; i < playTimeTop3.Count; i++)
			{
				var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == playTimeTop3[i].Name);
				users.Add(playTimeTop3[i].Name, record == null ? playTimeTop3[i].Playtime : playTimeTop3[i].Playtime + (int)(DateTime.UtcNow - record.TrackingStartTime).TotalSeconds);
			}

			commandContext.Reply($"#1: {users.ElementAt(0).Key} with {TimeSpan.FromSeconds(users.ElementAt(1).Value).Humanize(2, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} | " +
								$"#2: {users.ElementAt(1).Key} with {TimeSpan.FromSeconds(users.ElementAt(2).Value).Humanize(2, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)} | " +
								$"#3: {users.ElementAt(2).Key} with {TimeSpan.FromSeconds(users.ElementAt(3).Value).Humanize(2, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)}.");
		}

		[BotEvent(BotEventType.Command, "playcounttop")]
		public async Task OnPlaycountTopCommand(CommandContext commandContext)
		{
			var userDb = new UserDb();

			// Perhaps an optional parameter to get user at a certain spot?

			var playcountTop3 = await userDb.GetTopUsersByPlayCount(3);

			commandContext.Reply($"#1: {playcountTop3[0]?.Name} with {playcountTop3[0]?.Playcount} matches | " +
								$"#2: {playcountTop3[1]?.Name} with {playcountTop3[1]?.Playcount} matches | " +
								$"#3: {playcountTop3[2]?.Name} with {playcountTop3[2]?.Playcount} matches.");
		}

		[BotEvent(BotEventType.Command, "matchwinstop")]
		public async Task OnMatchWinsTopCommand(CommandContext commandContext)
		{
			var userDb = new UserDb();

			// Perhaps an optional parameter to get user at a certain spot?

			var playcountTop3 = await userDb.GetTopUsersByMatchWins(3);

			commandContext.Reply($"#1: {playcountTop3[0].Name} with {playcountTop3[0].MatchWins} wins | " +
								$"#2: {playcountTop3[1].Name} with {playcountTop3[1].MatchWins} wins | " +
								$"#3: {playcountTop3[2].Name} with {playcountTop3[2].MatchWins} wins.");
		}

		#endregion

			#region Map Stats

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

			string finalMessage = $"[https://osu.ppy.sh/b/{mapInfo.Id} {mapInfo.Name}] has been picked {pickCount} times.";


			if (lastScore != null)
				finalMessage += $" It was last played {lastScore.Time.Humanize(utcDate: true, culture: CultureInfo.InvariantCulture)}.";

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

			var set = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapSetAsync(map.SetId));

			commandContext.Reply($"{commandContext.Player.Name}'s most recent score in this lobby is a {latestScore.GetAccuracy():0.00}% {latestScore.Rank} rank with {latestScore.MaxCombo}x combo, {latestScore.Count300}/{latestScore.Count100}/{latestScore.Count50}/{latestScore.CountMiss} on [https://osu.ppy.sh/b/{latestScore.BeatmapId} {set?.Title}].");

			// TODO: Add PP calculations, probably want to store these in the score database save
		}

		#endregion

			#region Lobby Stats

		[BotEvent(BotEventType.Command, "totalplaytime")]
		public async void OnTotalPlaytimeCommand(CommandContext commandContext)
		{
			var userDb = new UserDb();

			var totalPlayTime = await userDb.GetTotalPlayTime();

			commandContext.Reply($"The total combined playtime of all players to join this lobby is {totalPlayTime.Humanize(4, maxUnit: TimeUnit.Hour, minUnit: TimeUnit.Second)}. " +
								$"That's over {Math.Floor(totalPlayTime.TotalDays)} days!");
		}

		[BotEvent(BotEventType.Command, "mostpicked")]
		public async void OnMostPickedCommand(CommandContext commandContext)
		{
			var matchDb = new MatchDb();

			var top3MostPlayed = matchDb.GetMostPlayedMaps(3);
			BeatmapExtended?[] top3Beatmaps =
			[
				await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync((int)top3MostPlayed[0].BeatmapId)),
				await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync((int)top3MostPlayed[1].BeatmapId)),
				await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync((int)top3MostPlayed[2].BeatmapId))
			];

			if (top3Beatmaps.Length < 3)
				return;

			BeatmapSetExtended?[] top3BeatmapSets =
			[
				await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapSetAsync(top3Beatmaps[0]!.SetId)),
				await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapSetAsync(top3Beatmaps[1]!.SetId)),
				await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapSetAsync(top3Beatmaps[2]!.SetId))
			];

			if (top3BeatmapSets.Length < 3 || top3BeatmapSets.Any(x => x == null))
				return;

			commandContext.Reply($"#1: [https://osu.ppy.sh/b/{top3MostPlayed[0].BeatmapId} {top3BeatmapSets[0]!.Title}] ({top3MostPlayed[0].PlayCount} times) | " +
								$"#2: [https://osu.ppy.sh/b/{top3MostPlayed[1].BeatmapId} {top3BeatmapSets[1]!.Title}] ({top3MostPlayed[1].PlayCount} times) | " +
								$"#3: [https://osu.ppy.sh/b/{top3MostPlayed[2].BeatmapId} {top3BeatmapSets[2]!.Title}]({top3MostPlayed[2].PlayCount} times).");

		}

		#endregion

			#region Other

		[BotEvent(BotEventType.Command, "admin")]
		public void OnAdminCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

			var userDb = new UserDb();

			if (commandContext.Player.Name.ToIrcNameFormat() == context.Lobby.LobbyConfiguration.LobbyAdminIrcUser)
			{
				if (commandContext.Args.Length > 0)
				{
					var user = userDb.FindUser(commandContext.Args[0]);

					if (user == null)
					{
						commandContext.Reply($"User '{commandContext.Args[0]}' not found.");
						return;
					}

					userDb.UpdateUserAdminStatus(user, true);

					commandContext.Reply($"User {user.Name} has been promoted to lobby administrator");
					return;
				}
			}
		}

		[BotEvent(BotEventType.Command, "removeadmin")]
		public void OnRemoveAdminCommand(CommandContext commandContext)
		{
			if (commandContext.Player == null)
				return;

			var userDb = new UserDb();

			if (commandContext.Player.Name.ToIrcNameFormat() == context.Lobby.LobbyConfiguration.LobbyAdminIrcUser)
			{
				if (commandContext.Args.Length > 0)
				{
					var user = userDb.FindUser(commandContext.Args[0]);

					if (user == null)
					{
						commandContext.Reply($"User '{commandContext.Args[0]}' not found.");
						return;
					}

					userDb.UpdateUserAdminStatus(user, false);

					commandContext.Reply($"User {user.Name}'s admin privileges have been removed indefinitely.");
					return;
				}
			}
		}

		#endregion

		#endregion

		#region Bot Events

		[BotEvent(BotEventType.PlayerJoined)]
		public void OnPlayerJoined(MultiplayerPlayer player)
		{
			Data.PlayerTimeRecords.Add(new MiscellaneousCommandsBehaviorData.PlayerTimeRecord
			{
				PlayerName = player.Name,
				TrackingStartTime = DateTime.UtcNow,
				InitialJoinTime = DateTime.UtcNow
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

			user.Playtime += (int)(DateTime.UtcNow - record.TrackingStartTime).TotalSeconds;

			await userDb.SaveAsync();

			Data.PlayerTimeRecords.Remove(record);

			// this should never be required, but clear records just in case.
			if (context.MultiplayerLobby.PlayerCount == 0)
				await ResetTimeRecords();
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
			await Task.Delay(5000);

			await Task.Run(async () =>
			{
				var recentScores = await GetRecentScores();

				await StoreMapData(recentScores);
				await StorePlayerFinishData(recentScores);
				await ResetTimeRecords();
				// Map leaderboard results?
			});
		}

		public async Task ResetTimeRecords()
		{
			Logger.Log(LogLevel.Trace, $"MiscellaneousCommandsBehavior: Resetting playtime records for {Data.PlayerTimeRecords.Count} users");

			await using var userDb = new UserDb();

			// Updates user stats from all active join records
			foreach (var record in Data.PlayerTimeRecords)
			{
				var user = await userDb.FindOrCreateUser(record.PlayerName);

				user.Playtime += (int)(DateTime.UtcNow - record.TrackingStartTime).TotalSeconds;
			}

			await userDb.SaveAsync();

			List<MiscellaneousCommandsBehaviorData.PlayerTimeRecord> previousTimeRecords = Data.PlayerTimeRecords.ToList();
			Data.PlayerTimeRecords.Clear();

			// Recreates join records for all players currently in lobby.
			foreach (var player in context.MultiplayerLobby.Players)
			{
				MiscellaneousCommandsBehaviorData.PlayerTimeRecord? previousTimeRecord = previousTimeRecords.FirstOrDefault(x => x.PlayerName == player.Name);

				Data.PlayerTimeRecords.Add(new MiscellaneousCommandsBehaviorData.PlayerTimeRecord
				{
					PlayerName = player.Name,
					TrackingStartTime = DateTime.UtcNow,
					InitialJoinTime = previousTimeRecord != null ? previousTimeRecord.InitialJoinTime : DateTime.UtcNow
				});
			}
		}

		#endregion

		#region Score Saving

		async Task<List<ScoreResult>> GetRecentScores()
		{
			var players = context.MultiplayerLobby.Players.Where(x => x.Id != null).ToList();
			var getScoreTasks = new List<Task<OsuSharp.Models.Scores.Score[]?>>();
			
			Logger.Log(LogLevel.Trace, $"Getting recent scores of players: {string.Join(", ", players.Select(x => x.Name))}");

			await context.Lobby.Bot.OsuApiClient.EnsureAccessTokenAsync();

			await using var userDb = new UserDb();

			for (int i = 0; i < players.Count; i++)
			{
				// We are sure the player has an Id at this point, update database entries
				await userDb.AssignUserId(players[i].Name, (int)players[i].Id!);

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
			if (Data.LastPlayedBeatmapInfo == null || Data.LastPlayedBeatmapInfo.Id == 0) return;

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

					if (score == null || score.Beatmap?.Id != map.BeatmapId)
						continue;

					var user = await userDb.FindOrCreateUser(result.Player.Name);

					await scoreDb.AddAsync(new Database.Entities.Score
					{
						UserId = user.Id,
						PlayerId = result.Player.Id,
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
