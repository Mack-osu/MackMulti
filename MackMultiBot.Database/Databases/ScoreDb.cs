using BanchoSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Storage;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;

namespace MackMultiBot.Database.Databases
{
	public class ScoreDb : BaseDatabase<Score>
	{
		/// <summary>
		/// Gets all scores of a user on a particular map.
		/// </summary>
		/// <param name="mapId">The osu! Beatmap ID of the map</param>
		/// <param name="playerId">The osu! User ID of the player.</param>
		/// <returns>A list of scores, sorted by recency</returns>
		public async Task<List<Score>?> GetMapScoresOfUserAsync(int mapId, int playerId)
		{
			return await _dbContext.Scores.
				Where(x => x.BeatmapId == mapId && x.PlayerId == playerId).
				OrderByDescending(x => x.Time).
				ToListAsync();
		}

		/// <summary>
		/// Gets all scores of a user.
		/// </summary>
		/// <param name="playerId">The osu! User ID of the player.</param>
		/// <returns>A list of scores, sorted by recency.</returns>
		public async Task<List<Score>?> GetScoresOfUserAsync(int playerId)
		{
			return await _dbContext.Scores.
				Where(x => x.PlayerId == playerId).
				OrderByDescending(x => x.Time).
				ToListAsync();
		}

		public async Task<Score?> GetBestMapScoreAsync(int beatmapId)
		{
			return await _dbContext.Scores.
				Where(x => x.BeatmapId == beatmapId).
				OrderByDescending(x => x.TotalScore).
				FirstOrDefaultAsync();
		}
	}
}
