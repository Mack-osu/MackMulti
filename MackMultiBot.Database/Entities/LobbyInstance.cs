using BanchoSharp.Multiplayer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Database.Entities
{
	public class LobbyInstance
	{
		public int Id { get; set; }

		public string Identifier { get; set; } = string.Empty;

		/// <summary>
		/// Channel name in the format "#mp_id"
		/// </summary>
		public string Channel { get; set; } = string.Empty;
	}
}
