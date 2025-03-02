using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using OsuSharp.Models.Beatmaps;
using MackMultiBot.Logging;
using MackMultiBot.Database.Databases;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class MapManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		readonly BehaviorDataProvider<MapManagerBehaviorData> _dataProvider = new(context.Lobby);
		private MapManagerBehaviorData Data => _dataProvider.Data;
		public async Task SaveData() => await _dataProvider.SaveData();

		#region Command Events

		[BotEvent(BotEventType.Command, "timeleft")]
		public void OnTimeLeftCommand(CommandContext commandContext)
		{
			if (commandContext.Lobby?.MultiplayerLobby == null || !commandContext.Lobby.MultiplayerLobby.MatchInProgress)
				return;

			TimeSpan timePassed = DateTime.UtcNow - Data.LastMatchStartTime;

			commandContext.Reply($"Estimated time left of current map: {(Data.BeatmapInfo.Length - timePassed):m\\:ss}");
		}

		[BotEvent(BotEventType.Command, "rules")]
		public void OnRulesCommand(CommandContext commandContext)
		{
			using var dbContext = new BotDatabaseContext();
			LobbyRuleConfiguration? ruleConfig = dbContext.LobbyRuleConfigurations.FirstOrDefault(x => x.LobbyConfigurationId == context.Lobby.LobbyConfigurationId);

			if (ruleConfig == null)
			{
				commandContext.Reply("This lobby has no rule configuration set up.");
				return;
			}
			if (ruleConfig.LimitDifficulty)
				commandContext.Reply($"Difficulty: {ruleConfig.MinimumDifficulty:0.00}* - {ruleConfig.MaximumDifficulty + ruleConfig.DifficultyMargin:0.00}*");

			if (ruleConfig.LimitMapLength)
				commandContext.Reply($"Map Length: {TimeSpan.FromSeconds(ruleConfig.MinimumMapLength):m\\:ss}" +
									$" - {TimeSpan.FromSeconds(ruleConfig.MaximumMapLength):m\\:ss}");
		}

		[BotEvent(BotEventType.Command, "mirror")]
		public void OnMirrorCommand(CommandContext commandContext)
		{
			int mapsetId = Data.BeatmapInfo.SetId;

			commandContext.Reply($"[https://beatconnect.io/b/{mapsetId} BeatConnect] | [https://osu.direct/d/{mapsetId} osu.direct] - [https://catboy.best/d/{mapsetId} catboy.best]");
		}

		#endregion

		#region Bot Events

		[BotEvent(BotEventType.MapChanged)]
		public async Task OnMapChanged(BeatmapShell beatmapShell)
		{
			// Prevents the bot from applying the same map repeatedly
			if (beatmapShell.Id == Data.LastSetBeatmapId)
				return;

			try
			{
				var beatmapInfo = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync(beatmapShell.Id));
				var beatmapAttributes = await context.UsingApiClient(async (apiClient) => await apiClient.GetDifficultyAttributesAsync(beatmapShell.Id));

				if (beatmapInfo == null)
				{
					Logger.Log(LogLevel.Info, $"MapManagerBehavior: Invalid beatmap selected with id {beatmapShell.Id}");
					context.SendMessage("Selected beatmap is not submitted, please try another one.");
					ApplyBeatmap(Data.LastSetBeatmapId);
					return;
				}

				if (beatmapAttributes == null)
				{
					Logger.Log(LogLevel.Warn, $"MapManagerBehavior: Failed to get beatmap information for map {beatmapShell.Id}");
					context.SendMessage("Error while trying to get beatmap information, please try another one.");
					ApplyBeatmap(Data.LastSetBeatmapId);
					return;
				}

				if (!await ValidateBeatmap(beatmapInfo, beatmapAttributes))
				{
					Logger.Log(LogLevel.Trace, "MapManagerBehavior: Invalid map chosen, reverting to latest valid pick");
					ApplyBeatmap(Data.LastSetBeatmapId);
					return;
				}

				var beatmapSet = (beatmapInfo as Beatmap).Set;

				Data.BeatmapInfo = new BeatmapInformation
				{
					Id = beatmapInfo.Id,
					SetId = beatmapInfo.SetId,
					Name = beatmapSet?.Title ?? string.Empty,
					Artist = beatmapSet?.Artist ?? string.Empty,
					Length = beatmapInfo.TotalLength,
					DrainLength = beatmapInfo.HitLength,
					StarRating = beatmapAttributes.DifficultyRating
				};

				ApplyBeatmap(beatmapInfo.Id); // have the bot set the map so that it is always on the latest version.
				SendBeatmapInfo(beatmapInfo, beatmapAttributes);
			}
			catch (HttpRequestException e)
			{
				Logger.Log(LogLevel.Error, $"MapManagerBehavior: Timed out getting beatmap information for map {beatmapShell.Id}, {e}");

				context.SendMessage("osu!api timed out while trying to get beatmap information");
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, $"MapManagerBehavior: Exception while trying to get beatmap information for map {beatmapShell.Id}, {e}");
				context.SendMessage("Internal error while trying to get beatmap information");
			}
		}

		[BotEvent(BotEventType.MatchStarted)]
		public void OnMatchStarted()
		{
			Data.LastMatchStartTime = DateTime.UtcNow;
		}

		#endregion

		#region Beatmap Management

		void SendBeatmapInfo(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes)
		{
			var beatmapSet = (beatmapInfo as Beatmap).Set;
			var roundedSr = Math.Round(difficultyAttributes.DifficultyRating, 2);

			context.SendMessage($"[https://osu.ppy.sh/b/{beatmapInfo.Id} {beatmapSet?.Artist} - {beatmapSet?.Title} [{beatmapInfo.Version}]] - [https://beatconnect.io/b/{beatmapInfo.Id} Beatconnect]");
			context.SendMessage($"Star Rating: {roundedSr} | Length: {beatmapInfo.TotalLength:mm\\:ss} | BPM: {beatmapInfo.BPM} | {beatmapInfo.Status}");
			context.SendMessage($"AR: {beatmapInfo.ApproachRate} | OD: {beatmapInfo.OverallDifficulty} | CS: {beatmapInfo.CircleSize} | HP: {beatmapInfo.CircleSize}");
		}

		/// <summary>
		/// Applies the provided beatmap id to the lobby, will also set the last bot applied beatmap id,
		/// so we don't end up in a loop of applying the same beatmap over and over.
		/// </summary>
		private void ApplyBeatmap(int beatmapId)
		{
			Data.LastSetBeatmapId = beatmapId;
			context.SendMessage($"!mp map {beatmapId.ToString()} 0");
		}

		async Task<bool> ValidateBeatmap(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes)
		{
			var hostQueueDataProvider = new BehaviorDataProvider<HostQueueBehaviorData>(context.Lobby);
			string hostName = hostQueueDataProvider.Data.Queue[0];

			await using var userDb = new UserDb();
			var hostUser = await userDb.FindOrCreateUser(hostName);

			if (hostUser.IsAdmin)
			{
				Logger.Log(LogLevel.Info, "MapManagerBehavior: Host is lobby admin, skipping beatmap validation");
				return true;
			}

			using var dbContext = new BotDatabaseContext();
			var lobbyRuleConfig = dbContext.LobbyRuleConfigurations.FirstOrDefault(x => x.LobbyConfigurationId == context.Lobby.LobbyConfigurationId);

			if (lobbyRuleConfig == null)
			{
				Logger.Log(LogLevel.Warn, "MapManagerBehavior: Lobby does not have a rule configuration. Map is valid by default");
				return true;
			}

			// Validate Star Rating
			if (lobbyRuleConfig.LimitDifficulty)
			{
				bool isWithinSrRange = !(difficultyAttributes.DifficultyRating > lobbyRuleConfig.MaximumDifficulty + lobbyRuleConfig.DifficultyMargin) 
										&& !(difficultyAttributes.DifficultyRating < lobbyRuleConfig.MinimumDifficulty);

				if (!isWithinSrRange)
				{
					context.SendMessage($"Selected beatmap is outside of difficulty range of the lobby: {lobbyRuleConfig.MinimumDifficulty:0.00}* - {(lobbyRuleConfig.MaximumDifficulty + lobbyRuleConfig.DifficultyMargin):0.00}*");
					return false;
				}
			}

			// Validate Map Length
			if (lobbyRuleConfig.LimitMapLength)
			{
				//bool isWithinLengthRange = !(beatmapInfo.TotalLength.TotalSeconds > lobbyRuleConfig.MaximumMapLength)
				//							&& !(beatmapInfo.TotalLength.TotalSeconds < lobbyRuleConfig.MinimumMapLength);

				if (beatmapInfo.TotalLength.TotalSeconds > lobbyRuleConfig.MaximumMapLength)
				{
					context.SendMessage($"Selected beatmap is too long for this lobby: " +
						$"{TimeSpan.FromSeconds(lobbyRuleConfig.MinimumMapLength):m\\:ss}" +
						$" - {TimeSpan.FromSeconds(lobbyRuleConfig.MaximumMapLength):m\\:ss}");
					return false;
				}

				if (beatmapInfo.TotalLength.TotalSeconds < lobbyRuleConfig.MinimumMapLength)
				{
					context.SendMessage($"Selected beatmap is too short for this lobby: " +
						$"{TimeSpan.FromSeconds(lobbyRuleConfig.MinimumMapLength):m\\:ss}" +
						$" - {TimeSpan.FromSeconds(lobbyRuleConfig.MaximumMapLength):m\\:ss}");
					return false;
				}
			}

			return true;
		}

		#endregion
	}
}
