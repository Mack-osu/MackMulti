using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Database.Entities
{
	public class PlayedMap
	{
		public int Id { get; set; }

		public long BeatmapId { get; set; }

		/// <summary>
		/// Map end time
		/// </summary>
		public DateTime Time { get; set; }
	}
}
