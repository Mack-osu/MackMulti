using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Database.Entities
{
	public class LobbyBehaviorData
	{
		public int Id { get; set; }

		[Required]
		public string LobbyIdentifier { get; set; } = string.Empty;

		[Required]
		public string BehaviorName { get; set; } = string.Empty;

		/// <summary>
		/// Dynamic JSON data for behaviour
		/// </summary>
		public string Data { get; set; } = string.Empty;
	}
}
