using BanchoSharp.Multiplayer;
using MackMultiBot.Bancho;
using MackMultiBot.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Interfaces
{
	public interface ILobby
	{
		public Bot Bot { get; init; }
		public BanchoConnection BanchoConnection { get; }
		public MultiplayerLobby? MultiplayerLobby { get; }
		public int LobbyConfigurationId { get; set; }

		public Task ConnectOrCreateAsync();

		/// <summary>
		/// Get Lobby configuration from database
		/// </summary>
		public Task<LobbyConfiguration> GetLobbyConfiguration();

	}
}
