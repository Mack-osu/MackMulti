using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;

namespace MackMultiBot.Behaviors
{
	public class LobbyManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
	{
		readonly BehaviorDataProvider<LobbyManagerBehaviorData> _dataProvider = new(context.Lobby);
		private LobbyManagerBehaviorData Data => _dataProvider.Data;

		public async Task SaveData() => await _dataProvider.SaveData();


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

		[BotEvent(BotEventType.Command, "close")]
		public async Task CloseLobby(CommandContext commandContext)
		{
			// Remove instance from lobby database
			await using var databaseCtx = new BotDatabaseContext();
			databaseCtx.LobbyInstances.Remove(databaseCtx.LobbyInstances.First(x => x.LobbyConfigurationId == context.Lobby.LobbyConfigurationId));
			databaseCtx.LobbyBehaviorData.RemoveRange(databaseCtx.LobbyBehaviorData.Where(x => x.LobbyConfigurationId == context.Lobby.LobbyConfigurationId));

			await databaseCtx.SaveChangesAsync();

			commandContext.Reply("!mp close");
		}

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
	}
}
