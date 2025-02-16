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

namespace MackMulti.Database.Databases
{
	public class UserDb : BaseDatabase<User>
	{
		public async Task<User> FindOrCreateUser(string username)
		{
			return FindUser(username) ?? await CreateUser(username);
		}

		User? FindUser(string username)
		{
			string formattedUsername = username.ToIrcNameFormat();
			return _dbContext.Users
				.AsEnumerable() // Pulls data into memory
				.FirstOrDefault(x => x.Name.ToIrcNameFormat() == formattedUsername);
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

			Console.WriteLine($"User '{user.Name}' IsAdmin status updated to {status}.");
		}

		public async void UpdateUserAdminStatus(string username, bool status)
		{
			User user = FindOrCreateUser(username).Result;

			if (user == null)
				return;

			user.IsAdmin = status;
			await SaveAsync();

			Console.WriteLine($"User '{user.Name}' IsAdmin status updated to {status}.");
		}

		public async void UpdateUserAutoskipStatus(User user, bool status)
		{
			user = FindOrCreateUser(user.Name).Result; // For some reason it's finding an empty user entry without this line.

			user.AutoSkip = status;
			await SaveAsync();

			Console.WriteLine($"UserDb: User '{user.Name}' AutoSkip status updated to {status}.");
		}
	}
}
