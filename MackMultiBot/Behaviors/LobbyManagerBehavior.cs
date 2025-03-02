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
			commandContext.Reply("Attention: Lobby will be closing in 30 seconds. Thank you for playing!");
			await Task.Delay(20000);

			commandContext.Reply("Attention: Lobby will be closing in 10 seconds. Thank you for playing!");
			await Task.Delay(10000);

			foreach (IMultiplayerPlayer player in context.Lobby.MultiplayerLobby.Players)
			{
				commandContext.Reply($"!mp kick {(player.Id != null ? player.Id : player.Name.ToIrcNameFormat())}");
			}

			await Task.Delay(10000);

			databaseCtx.LobbyInstances.Remove(databaseCtx.LobbyInstances.First(x => x.LobbyConfigurationId == context.Lobby.LobbyConfigurationId));
			databaseCtx.LobbyBehaviorData.RemoveRange(databaseCtx.LobbyBehaviorData.Where(x => x.LobbyConfigurationId == context.Lobby.LobbyConfigurationId));

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
			commandContext.Reply($"All available command can be found on [https://osu.ppy.sh/users/11584934 my profile]");
		}

		#endregion

		#region Bancho Events

		[BotEvent(BotEventType.Initialize)]
		public async Task OnInitialize()
		{
			if (Data.IsFreshInstance)
			{
				await OnMatchFinished(); // Ensures room config
				Data.IsFreshInstance = false;
				return;
			}

			context.SendMessage("!mp settings");
		}

		[BotEvent(BotEventType.MatchFinished)]
		public async Task OnMatchFinished()
		{
			var lobbyConfig = await context.Lobby.GetLobbyConfiguration();

			EnsureRoomName(lobbyConfig);
			EnsureRoomPassword(lobbyConfig);
			EnsureMatchSettings(lobbyConfig);
			EnsureMatchMods(lobbyConfig);
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

		// Stolen from matte :)
		void EnsureMatchMods(LobbyConfiguration configuration)
		{
			if (configuration.Mods == null)
			{
				return;
			}

			// No, I can't read this easily either, but it's short. :)
			// Good example of bad code, but it's not worth refactoring.
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

		// Stolen from matte :)
		private static string GenerateModsCommand(string modsCommandNonSpacing)
		{
			// TODO: Move this madness elsewhere, it probably shouldn't be here.
			// We need to translate the mods command to the format that bancho expects.
			// For example "!mp mods HR HD"

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
	}
}
