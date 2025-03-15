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
	public class MatchDb : BaseDatabase<PlayedMap>
	{
		public async Task<PlayedMap?> GetLastPlayedByMapIdAsync(int beatmapId)
		{
			return await _dbContext.PlayedMaps.Where(x => x.BeatmapId == beatmapId).OrderByDescending(x => x.Time).FirstOrDefaultAsync();
		}

		public async Task<int> GetMatchCountByMapIdAsync(int beatmapId)
		{
			return await _dbContext.PlayedMaps.Where(x => x.BeatmapId == beatmapId).CountAsync();
		}

		public List<MostPlayedEntry> GetMostPlayedMaps(int count)
		{
			// Group by BeatmapId, count the occurrences, sort by the count in descending order, and take the top 'count' entries
			return _dbContext.PlayedMaps
				.GroupBy(entry => entry.BeatmapId)
				.Select(group => new MostPlayedEntry() { BeatmapId = group.Key, PlayCount = group.Count()})
				.OrderByDescending(x => x.PlayCount)
				.Take(count)
				.ToList();
		}
	}

	public class MostPlayedEntry
	{
		public long BeatmapId;
		public int PlayCount;
	}
}
