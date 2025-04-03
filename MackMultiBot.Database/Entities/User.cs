using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Database.Entities
{
	public class User
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public int UserId { get; set; } = 0;
		public bool IsAdmin { get; set; }
		public bool AutoSkip { get; set; }
		public int Playtime { get; set; }
		public int Playcount { get; set; }
		public int MatchWins { get; set; }
	}
}
