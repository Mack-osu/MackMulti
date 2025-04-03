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
				.AsEnumerable()
				.FirstOrDefault(x => x.Name.ToIrcNameFormat() == formattedUsername);
		}

        #region Lobby Statistics

        public async Task<IReadOnlyList<User>> GetTopUsersByPlayTime(int count)
		{
			return await _dbContext.Users.OrderByDescending(x => x.Playtime).
				Take(count).
				ToListAsync();
		}

		public async Task<IReadOnlyList<User>> GetTopUsersByPlayCount(int count)
		{
			return await _dbContext.Users.OrderByDescending(x => x.Playcount).
				Take(count).
				ToListAsync();
		}

		public async Task<IReadOnlyList<User>> GetTopUsersByMatchWins(int count)
		{
			return await _dbContext.Users.OrderByDescending(x => x.MatchWins).
				Take(count).
				ToListAsync();
		}

		public async Task<int> GetUserPlaytimeSpot(string username)
		{
			return (await _dbContext.Users.OrderByDescending(x => x.Playtime).ToListAsync()).FindIndex(x => x.Name.ToIrcNameFormat() == username.ToIrcNameFormat());
		}

		public async Task<int> GetUserMatchWinsSpot(string username)
		{
			return (await _dbContext.Users.OrderByDescending(x => x.MatchWins).ToListAsync()).FindIndex(x => x.Name.ToIrcNameFormat() == username.ToIrcNameFormat());
		}

		public async Task<int> GetUserPlaycountSpot(string username)
		{
			return (await _dbContext.Users.OrderByDescending(x => x.Playcount).ToListAsync()).FindIndex(x => x.Name.ToIrcNameFormat() == username.ToIrcNameFormat());
		}

		public async Task<TimeSpan> GetTotalPlayTime()
		{
			return TimeSpan.FromSeconds(await _dbContext.Users.SumAsync(x => x.Playtime));
		}

        #endregion

		public async Task AssignUserId(string username, int userId)
        {
			var user = await FindOrCreateUser(username);

            if (user.UserId != 0)
				return;

            Logger.Log(LogLevel.Info, $"UserDb: Assigning user ID '{userId}' to user '{username}'");

            user.UserId = userId;

			await SaveAsync();

            UpdateUsernameFromOsuId(userId);
		}

		async void UpdateUsernameFromOsuId(int userId)
		{
			if (userId == 0)
				return;

			Logger.Log(LogLevel.Info, $"UserDb: Updating username of player with ID '{userId}'");

			var query = _dbContext.Users.Where(x => x.UserId == userId);

			if (query.Count() >= 2)
            {
				query = query.OrderBy(x => x.Id);
                query.First().Name = query.Last().Name;
				_dbContext.Remove(query.Last());
			}

			await SaveAsync();
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
			user = await FindOrCreateUser(user.Name); // For some reason it's finding an empty user entry without this line.

			user.AutoSkip = status;
			await SaveAsync();

			Logger.Log(LogLevel.Info, $"UserDb: User '{user.Name}' AutoSkip status updated to {status}.");
		}
	}
}
