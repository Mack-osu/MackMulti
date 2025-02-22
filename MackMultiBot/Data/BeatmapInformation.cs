using BanchoSharp.Multiplayer;
using OsuSharp.Models.Scores;

namespace MackMultiBot.Data
{
	public class BeatmapInformation
	{
		public int Id { get; set; }

		public int SetId { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Artist { get; set; } = string.Empty;

		public TimeSpan Length { get; set; }

		public TimeSpan DrainLength { get; set; }

		public float StarRating { get; set; }
	}
}
