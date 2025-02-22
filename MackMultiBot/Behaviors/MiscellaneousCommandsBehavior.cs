using BanchoSharp.Multiplayer;
using Humanizer;
using Humanizer.Localisation;
using MackMulti.Database.Databases;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Interfaces;
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
	}
}
