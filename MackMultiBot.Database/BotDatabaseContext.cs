using MackMultiBot.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace MackMultiBot.Database
{
	public class BotDatabaseContext : DbContext
	{
		public static string? ConnectionString;
		public DbSet<LobbyInstance> LobbyInstances => Set<LobbyInstance>();
		public DbSet<User> Users => Set<User>();
		public DbSet<LobbyBehaviorData> LobbyBehaviorData => Set<LobbyBehaviorData>();
		public DbSet<PlayedMap> PlayedMaps => Set<PlayedMap>();
		public DbSet<Score> Scores => Set<Score>();

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (ConnectionString == null)
				throw new InvalidOperationException("No connection string set");

			optionsBuilder.UseSqlite(ConnectionString);

			//optionsBuilder.UseSqlite("Data Source=E:/temp/MackMulti/Builds/DevBuild/data.db");

		}
	}
}
