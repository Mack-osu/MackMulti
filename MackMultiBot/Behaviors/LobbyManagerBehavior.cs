using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Databases;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;

namespace MackMultiBot.Behaviors
{
	public class LobbyManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		readonly BehaviorDataProvider<LobbyManagerBehaviorData> _dataProvider = new(context.Lobby);
		private LobbyManagerBehaviorData Data => _dataProvider.Data;

		public async Task SaveData() => await _dataProvider.SaveData();

		#region Command Events

		// Very ugly at the moment, but that's alright. Will rework once a timer system is in place.
		[BotEvent(BotEventType.Command, "close")]
		public async Task CloseLobby(CommandContext commandContext)
		{
			// Remove instance from lobby database
			await using var databaseCtx = new BotDatabaseContext();

			if (context?.Lobby?.MultiplayerLobby == null)
			{
				Logger.Log(LogLevel.Warn, "LobbyManagerBehavior: No lobby found during lobby closing sequence.");
				return;
			}

			commandContext.Reply("!mp clearhost");
			commandContext.Reply("!mp password v27B62yE6"); // Random letters :)

			foreach (IMultiplayerPlayer player in context.Lobby.MultiplayerLobby.Players)
			{
				commandContext.Reply($"!mp kick {(player.Id != null ? "#" + player.Id : player.Name.ToIrcNameFormat())}");
			}

			await Task.Delay(10000);

			databaseCtx.LobbyInstances.Remove(databaseCtx.LobbyInstances.First());
			databaseCtx.LobbyBehaviorData.RemoveRange(databaseCtx.LobbyBehaviorData);

			await databaseCtx.SaveChangesAsync();

			commandContext.Reply("!mp close");
		}

		[BotEvent(BotEventType.Command, "mplink")]
		public void OnMpLinkCommand(CommandContext commandContext)
		{
			if (commandContext.Lobby?.MultiplayerLobby == null)
			{
				Logger.Log(LogLevel.Warn, "LobbyManagerBehavior: No lobby found while executing mplink command");
				return;
			}

			commandContext.Reply($"Match history available [https://osu.ppy.sh/mp/{commandContext.Lobby.MultiplayerLobby.Id} here]");
		}

		[BotEvent(BotEventType.Command, "help")]
		public void OnHelpCommand(CommandContext commandContext)
		{
			commandContext.Reply($"All available commands can be found on [https://osu.ppy.sh/users/11584934 my profile]");
		}

		#endregion

		#region Bancho Events

		[BotEvent(BotEventType.Initialize)]
		public void OnInitialize()
		{
			if (Data.IsFreshInstance)
			{
				OnMatchFinished(); // Ensures room config
				Data.IsFreshInstance = false;
				return;
			}

			context.SendMessage("!mp settings");
		}

		[BotEvent(BotEventType.MatchFinished)]
		public async void OnMatchFinished()
		{
			// Artificial delay, hoping this fixes the issue where settings are sometimes set to for example !mp password PRIVMSG!mp password e and so on
			EnsureRoomName(context.Lobby.LobbyConfiguration);
			await Task.Delay(50);
			EnsureRoomPassword(context.Lobby.LobbyConfiguration);
			await Task.Delay(50);
			EnsureMatchSettings(context.Lobby.LobbyConfiguration);
			await Task.Delay(50);
			EnsureMatchMods(context.Lobby.LobbyConfiguration);
		}

		#endregion

		#region Lobby Management

		void EnsureRoomName(LobbyConfiguration configuration)
		{
			if (context.MultiplayerLobby.Name == configuration.Name)
				return;

			context.SendMessage($"!mp name {configuration.Name}");
		}

		void EnsureRoomPassword(LobbyConfiguration configuration)
		{
			context.SendMessage($"!mp password {configuration.Password ?? ""}");
		}

		void EnsureMatchSettings(LobbyConfiguration configuration)
		{
			var teamMode = ((int)(configuration.TeamMode ?? LobbyFormat.HeadToHead)).ToString();
			var scoreMode = ((int)(configuration.ScoreMode ?? WinCondition.Score)).ToString();
			var size = configuration.Size.ToString() ?? "16";

			context.SendMessage($"!mp set {teamMode} {scoreMode} {size}");
		}

		[BotEvent(BotEventType.Command, "enforcemods")]
		public void OnEnforceModsCommand(CommandContext commandContext)
		{
			OnMatchFinished();
		}

		// Stolen from: https://github.com/matte-ek/BanchoMultiplayerBot/tree/master
		void EnsureMatchMods(LobbyConfiguration configuration)
		{
			if (configuration.Mods == null)
			{
				return;
			}

			Mods desiredMods = configuration.Mods.Aggregate<string, Mods>(0, (current, modName) => current | (Mods)Enum.Parse(typeof(Mods), modName));

			if (context.MultiplayerLobby.Mods == desiredMods)
			{
				return;
			}

			var modsCommandNonSpacing = desiredMods.ToAbbreviatedForm(false);

			if (modsCommandNonSpacing == "None")
			{
				if ((desiredMods & Mods.Freemod) != 0)
				{
					context.SendMessage("!mp mods Freemod");
				}

				return;
			}

			context.SendMessage($"!mp mods {GenerateModsCommand(modsCommandNonSpacing)}");
		}

		// Stolen from: https://github.com/matte-ek/BanchoMultiplayerBot/tree/master
		private static string GenerateModsCommand(string modsCommandNonSpacing)
		{
			var modsCommand = "";
			bool newMod = false;

			foreach (var c in modsCommandNonSpacing)
			{
				modsCommand += c;

				if (newMod)
				{
					modsCommand += ' ';
					newMod = false;
					continue;
				}

				newMod = true;
			}

			return modsCommand;
		}

		#endregion

		[BotEvent(BotEventType.TimerFinished, "LobbyWatchTimer")]
		public void OnWatchTimerElapsed()
		{
			// Temporary direct setting of name until I add functionality to await an !mp settings call.
			context.SendMessage($"!mp name {context.Lobby.LobbyConfiguration.Name}");

			OnMatchFinished();
		}
	}
}
