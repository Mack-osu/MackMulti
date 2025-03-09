using BanchoSharp.Multiplayer;
using MackMultiBot.Behaviors.Data;
using MackMultiBot.Database;
using MackMultiBot.Database.Databases;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using OsuSharp.Models.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public class BeatmapValidator(ILobby lobby, LobbyRuleConfiguration lobbyRuleConfiguration)
	{
		public async Task<MapValidationResult> ValidateBeatmap(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes, bool useDifficultyAttributes)
		{
			if (await IsHostAdmin())
				return MapValidationResult.Valid;

			if (!IsValidDifficulty(beatmapInfo, difficultyAttributes, useDifficultyAttributes))
				return MapValidationResult.InvalidDifficulty;

			if (!IsValidMapLength(beatmapInfo))
				return MapValidationResult.InvalidMapLength;
			
			// Gamemode
			
			// Map bans?

			return MapValidationResult.Valid;
		}

		async Task<bool> IsHostAdmin()
		{
			if (lobby.MultiplayerLobby?.Host == null)
				return false;

			await using var userDb = new UserDb();

			var user = await userDb.FindOrCreateUser(lobby.MultiplayerLobby!.Host.Name);

			return user.IsAdmin;
		}

		bool IsValidDifficulty(BeatmapExtended beatmapInfo, DifficultyAttributes difficultyAttributes, bool useDifficultyAttributes)
		{
			if (lobbyRuleConfiguration == null)
				return true;

			var SR = !useDifficultyAttributes ? beatmapInfo?.DifficultyRating : difficultyAttributes.DifficultyRating;

			return (SR <= lobbyRuleConfiguration.MaximumDifficulty + lobbyRuleConfiguration.DifficultyMargin)
					&& (SR >= lobbyRuleConfiguration.MinimumDifficulty);
		}

		bool IsValidMapLength(BeatmapExtended beatmapInfo)
		{
			if (lobbyRuleConfiguration == null)
				return true;

			return !(beatmapInfo.TotalLength.TotalSeconds > lobbyRuleConfiguration.MaximumMapLength)
					&& !(beatmapInfo.TotalLength.TotalSeconds < lobbyRuleConfiguration.MinimumMapLength);
		}
	}

	public enum MapValidationResult
	{
		Valid,
		Invalid,
		Error,
		InvalidDifficulty,
		InvalidMapLength
	}
}
