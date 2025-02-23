using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Data;
using MackMultiBot.Interfaces;
using OsuSharp.Models.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class MapManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("MapManagerBehaviorLogger");

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
					_logger.Info("MapManagerBehavior: Invalid beatmap with id {BeatmapId}", beatmapShell.Id);
					context.SendMessage("Selected beatmap is not submitted, please try another one.");
					return;
				}

				if (beatmapAttributes == null)
				{
					_logger.Error("MapManagerBehavior: Failed to get beatmap information for map {BeatmapId}", beatmapShell.Id);
					context.SendMessage("error while trying to get beatmap information.");
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
				_logger.Error("MapManagerBehavior: Timed out getting beatmap information for map {BeatmapId}, {e}", beatmapShell.Id, e);

				context.SendMessage("osu!api timed out while trying to get beatmap information");
			}
			catch (Exception e)
			{
				_logger.Error(e, "MapManagerBehavior: Exception while trying to get beatmap information for map {BeatmapId}, {e}", beatmapShell.Id, e);

				context.SendMessage("Internal error while trying to get beatmap information");
			}
		}

		[BotEvent(BotEventType.MatchStarted)]
		public void OnMatchStarted()
		{
			Data.LastMatchStartTime = DateTime.UtcNow;
		}

		#endregion

		void SendBeatmapInfo(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes)
		{
			var beatmapSet = (beatmapInfo as Beatmap).Set;
			var roundedSr = Math.Round(difficultyAttributes.DifficultyRating, 2);

			context.SendMessage($"[https://osu.ppy.sh/b/{beatmapInfo.Id} {beatmapSet?.Artist} - {beatmapSet?.Title} [{beatmapInfo.Version}]] - [https://beatconnect.io/b/{beatmapInfo.Id} Beatconnect]");
			context.SendMessage($"Star Rating: {roundedSr} | Length: {beatmapInfo.TotalLength:mm\\:ss} | BPM: {beatmapInfo.BPM} | {beatmapInfo.Status}");
			context.SendMessage($"AR: {beatmapInfo.ApproachRate} | OD: {beatmapInfo.OverallDifficulty} | CS: {beatmapInfo.CircleSize} | HP: {beatmapInfo.CircleSize}");
			// Api calls for map info such as AR, Star rating, mapper, etc.
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

	}
}
