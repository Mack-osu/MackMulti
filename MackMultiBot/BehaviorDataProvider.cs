using MackMultiBot.Database;
using MackMultiBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public sealed class BehaviorDataProvider<T> where T : class
	{
		static NLog.Logger _logger = NLog.LogManager.GetLogger("BehaviorDataProviderLogger");

		public readonly T Data = null!;

		private readonly ILobby _lobby;

		public BehaviorDataProvider(ILobby lobby)
		{
			_lobby = lobby;

			using var dbContext = new BotDatabaseContext();

			var typeName = typeof(T).Name;

			var data = dbContext.LobbyBehaviorData.FirstOrDefault(x => x.LobbyConfigurationId == lobby.LobbyConfigurationId && x.BehaviorName == typeName);
			if (data == null)
			{
				_logger.Trace("BehaviorDataProvider: Unable to find data for {BehaviorDataType}, creating new one", typeName);

				Data = (T)Activator.CreateInstance(typeof(T))!;

				return;
			}

			Data = JsonConvert.DeserializeObject<T>(data.Data) ?? throw new InvalidOperationException();
		}

		public async Task SaveData()
		{
			await using var dbContext = new BotDatabaseContext();

			var typeName = typeof(T).Name;

			var data = await dbContext.LobbyBehaviorData.FirstOrDefaultAsync(x => x.LobbyConfigurationId == _lobby.LobbyConfigurationId && x.BehaviorName == typeName);

			if (data == null)
			{
				data = new Database.Entities.LobbyBehaviorData
				{
					LobbyConfigurationId = _lobby.LobbyConfigurationId,
					BehaviorName = typeName,
					Data = JsonConvert.SerializeObject(Data)
				};

				dbContext.LobbyBehaviorData.Add(data);

				await dbContext.SaveChangesAsync();

				return;
			}

			data.Data = JsonConvert.SerializeObject(Data);

			await dbContext.SaveChangesAsync();
		}
	}
}
