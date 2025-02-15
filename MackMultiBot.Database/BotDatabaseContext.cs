using MackMultiBot.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace MackMultiBot.Database
{
	public class BotDatabaseContext : DbContext
	{
		string ConnectionString { get; set; } = "Data Source=E:/Coding/MackMulti/MackMultiBot/MackMultiBot.Database/data.db";

		public DbSet<LobbyInstance> LobbyInstances => Set<LobbyInstance>();
		public DbSet<LobbyConfiguration> LobbyConfigurations => Set<LobbyConfiguration>();

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite(ConnectionString);
		}
	}
}
