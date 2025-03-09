using MackMultiBot.Database.Entities;
using MackMultiBot.Logging;
using Microsoft.EntityFrameworkCore;

namespace MackMultiBot.Database.Databases
{
	public class UserDb : BaseDatabase<User>
	{
		public async Task<User> FindOrCreateUser(string username)
		{
			return FindUser(username) ?? await CreateUser(username);
		}

		public User? GetUserFromDbIndex(int index)
		{
			return _dbContext.Users.FirstOrDefault(x => x.Id == index);
		}

		public User? FindUser(string username)
		{
			string formattedUsername = username.ToIrcNameFormat();
			return _dbContext.Users
				.AsEnumerable() // Pulls data into memory
				.FirstOrDefault(x => x.Name.ToIrcNameFormat() == formattedUsername);
		}

		public async Task<IReadOnlyList<User>> GetTopUsersByPlayTime(int count)
		{
			return await _dbContext.Users.OrderByDescending(x => x.Playtime).
				Take(count).
				ToListAsync();
		}

		public async Task<int> GetUserPlaytimeSpot(string username)
		{
			return (await _dbContext.Users.OrderByDescending(x => x.Playtime).ToListAsync()).FindIndex(x => x.Name.ToIrcNameFormat() == username.ToIrcNameFormat());
		}

		async Task<User> CreateUser(string username)
		{
			var user = new User()
			{
				Name = username,
			};

			await AddAsync(user);
			await SaveAsync();

			return user;
		}

		public async void UpdateUserAdminStatus(User user, bool status)
		{
			user.IsAdmin = status;
			await SaveAsync();

			Logger.Log(LogLevel.Info, $"User '{user.Name}' IsAdmin status updated to {status}.");
		}

		public async void UpdateUserAdminStatus(string username, bool status)
		{
			User user = FindOrCreateUser(username).Result;

			if (user == null)
				return;

			user.IsAdmin = status;
			await SaveAsync();

			Logger.Log(LogLevel.Info, $"User '{user.Name}' IsAdmin status updated to {status}.");
		}

		public async void UpdateUserAutoskipStatus(User user, bool status)
		{
			user = FindOrCreateUser(user.Name).Result; // For some reason it's finding an empty user entry without this line.

			user.AutoSkip = status;
			await SaveAsync();

			Logger.Log(LogLevel.Info, $"UserDb: User '{user.Name}' AutoSkip status updated to {status}.");
		}
	}
}
