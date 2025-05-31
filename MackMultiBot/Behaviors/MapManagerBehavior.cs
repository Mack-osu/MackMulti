using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using OsuSharp.Models.Beatmaps;
using MackMultiBot.Logging;
using MackMultiBot.Database.Databases;
using System.Linq;

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
			if (commandContext.Lobby?.MultiplayerLobby == null || !commandContext.Lobby.MultiplayerLobby.MatchInProgress || commandContext.Player == null)
				return;


            TimeSpan timePassed = DateTime.UtcNow - Data.LastMatchStartTime;

			string message = $"Estimated time left of current map: {(Data.BeatmapInfo.Length - timePassed):m\\:ss}";

            if (commandContext.Args.Length >= 1 && commandContext.Args[0].ToLower() == "ping")
			{
				Data.PlayersToPing.Add(commandContext.Player.Name.ToIrcNameFormat());
                message += ". You will be pinged when the match has finished.";
			}

            commandContext.Reply(message);
        }

		[BotEvent(BotEventType.Command, "rules")]
		public void OnRulesCommand(CommandContext commandContext)
		{
			var ruleConfig = commandContext.Lobby?.LobbyConfiguration.RuleConfig;

			if (ruleConfig == null)
			{
				commandContext.Reply("This lobby has no rule configuration set up.");
				return;
			}
			Console.WriteLine(ruleConfig.LimitDifficulty);
			Console.WriteLine(ruleConfig.LimitMapLength);

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

			commandContext.Reply($"[https://beatconnect.io/b/{mapsetId} BeatConnect] | [https://osu.direct/d/{mapsetId} osu.direct] | [https://catboy.best/d/{mapsetId} catboy.best]");
		}

		#endregion

		#region Bot Events

		[BotEvent(BotEventType.MapChanged)]
		public async Task OnMapChanged(BeatmapShell beatmapShell)
		{
			// Prevents the bot from applying the same map repeatedly
			if (beatmapShell.Id == Data.LastBotAppliedBeatmapId)
				return;

			try
			{
				var beatmapInfo = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync(beatmapShell.Id));
				var difficultyAttributes = await context.UsingApiClient(async (apiClient) => await apiClient.GetDifficultyAttributesAsync(beatmapShell.Id));

				if (beatmapInfo == null)
				{
					Logger.Log(LogLevel.Info, $"MapManagerBehavior: Invalid beatmap selected with id {beatmapShell.Id}");
					context.SendMessage("Selected beatmap is not submitted, please try another one.");
					ApplyBeatmap(Data.LastValidBeatmapId);
					return;
				}

				if (difficultyAttributes == null)
				{
					Logger.Log(LogLevel.Warn, $"MapManagerBehavior: Failed to get beatmap information for map {beatmapShell.Id}");
					context.SendMessage("Error while trying to get beatmap information, please try another one.");
					ApplyBeatmap(Data.LastValidBeatmapId);
					return;
				}

				// Map validation
				using var dbContext = new BotDatabaseContext();
				var lobbyRuleConfig = context.Lobby.LobbyConfiguration.RuleConfig;
				var beatmapValidator = new BeatmapValidator(context.Lobby, lobbyRuleConfig!);
				var validationResult = await beatmapValidator.ValidateBeatmap(beatmapInfo, difficultyAttributes, (context.MultiplayerLobby.Mods & Mods.Freemod) != 0);

				// DT?
				if (validationResult == MapValidationResult.InvalidDifficulty && lobbyRuleConfig!.MinimumDifficulty > Math.Round(difficultyAttributes.DifficultyRating, 2))
				{
					var dtDifficultyAttributes = await context.UsingApiClient(async (apiClient) => await apiClient.GetDifficultyAttributesAsync(beatmapShell.Id, ["DT"])); 
					
					// Is map valid with DT?
					if (dtDifficultyAttributes != null && await beatmapValidator.ValidateBeatmap(beatmapInfo, dtDifficultyAttributes, (context.MultiplayerLobby.Mods & Mods.Freemod) != 0) == MapValidationResult.Valid)
					{
						// Does room have DT selected?
						context.SendMessage("!mp settings"); // Find a way to await execution of this
						await Task.Delay(2500);

						if ((context.MultiplayerLobby.Mods & Mods.DoubleTime) != 0)
						{
							await EnforceLobbyRules(beatmapInfo, dtDifficultyAttributes, MapValidationResult.Valid, lobbyRuleConfig, (int)Mods.DoubleTime);
							return;
						}
					}
				}

				await EnforceLobbyRules(beatmapInfo, difficultyAttributes, validationResult, lobbyRuleConfig!);
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

		[BotEvent(BotEventType.MatchFinished)]
		public void OnMatchFinished()
		{
			var lobbyName = context.Lobby.LobbyConfiguration.Name;

			foreach (var username in Data.PlayersToPing)
			{
				context.Lobby.BanchoConnection.MessageHandler.SendMessage($"{username}", $"The match in {lobbyName}] has finished, you will receive an invite shortly.");
				context.MultiplayerLobby.InviteAsync(username);
			}

			Data.PlayersToPing.Clear();
		}

		#endregion

		#region Beatmap Management

		void SendBeatmapInfo(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes)
		{
			var beatmapSet = (beatmapInfo as Beatmap).Set;
			var roundedSr = Math.Round(difficultyAttributes.DifficultyRating, 2);

			context.SendMessage($"[https://osu.ppy.sh/b/{beatmapInfo.Id} {beatmapSet?.Artist} - {beatmapSet?.Title} [{beatmapInfo.Version}]] - [https://catboy.best/d/{beatmapInfo.SetId} Mirror]");
			context.SendMessage($"Star Rating: {roundedSr} | Length: {beatmapInfo.TotalLength:mm\\:ss} | BPM: {beatmapInfo.BPM} | {beatmapInfo.Status}");
			context.SendMessage($"AR: {beatmapInfo.ApproachRate} | OD: {beatmapInfo.OverallDifficulty} | CS: {beatmapInfo.CircleSize} | HP: {beatmapInfo.HealthDrain}");
		}

		/// <summary>
		/// Applies the provided beatmap id to the lobby, will also set the last bot applied beatmap id,
		/// so we don't end up in a loop of applying the same beatmap over and over.
		/// </summary>
		void ApplyBeatmap(int beatmapId)
		{
			Data.LastBotAppliedBeatmapId = beatmapId;
			context.SendMessage($"!mp map {beatmapId} 0");
		}

		async Task EnforceLobbyRules(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes, MapValidationResult validationResult, LobbyRuleConfiguration lobbyRuleConfig, int mods = 0)
		{
			var beatmapSet = (beatmapInfo as Beatmap).Set;

			Logger.Log(LogLevel.Trace, $"MapManagerBehavior: Enforcing beatmap regulations for map {beatmapInfo.Id}, status: {validationResult}");

			if (validationResult == MapValidationResult.Valid)
			{
				Data.BeatmapInfo = new BeatmapInformation
				{
					Id = beatmapInfo.Id,
					SetId = beatmapInfo.SetId,
					Name = beatmapSet?.Title ?? string.Empty,
					Artist = beatmapSet?.Artist ?? string.Empty,
					Length = beatmapInfo.TotalLength,
					DrainLength = beatmapInfo.HitLength,
					StarRating = difficultyAttributes.DifficultyRating
				};

				Data.LastValidBeatmapId = Data.BeatmapInfo.Id;

				await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("NewMap");

				ApplyBeatmap(beatmapInfo.Id); // have the bot set the map so that it is always on the latest version.

				SendBeatmapInfo(beatmapInfo, difficultyAttributes);
				return;
			}

			ApplyBeatmap(Data.LastValidBeatmapId);

			switch (validationResult)
			{
				case MapValidationResult.InvalidDifficulty:
					context.SendMessage($"Selected beatmap ({Math.Round(difficultyAttributes.DifficultyRating, 2)}*) is outside of difficulty range of the lobby: {lobbyRuleConfig!.MinimumDifficulty:0.00}* - {(lobbyRuleConfig.MaximumDifficulty + lobbyRuleConfig.DifficultyMargin):0.00}*");
					return;

				case MapValidationResult.InvalidMapLength:
					context.SendMessage($"Selected beatmap ({beatmapInfo.TotalLength:m\\:ss}) is outside length range of the lobby: " +
						$"{TimeSpan.FromSeconds(lobbyRuleConfig!.MinimumMapLength):m\\:ss}" +
						$" - {TimeSpan.FromSeconds(lobbyRuleConfig.MaximumMapLength):m\\:ss}");
					return;
			}
		}

		#endregion
	}
}
