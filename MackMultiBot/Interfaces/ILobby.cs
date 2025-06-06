﻿using BanchoSharp.Multiplayer;
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
		public LobbyConfiguration LobbyConfiguration { get; }
		public BehaviorEventProcessor? BehaviorEventProcessor { get; }
		public ITimerHandler? TimerHandler { get; }
		public IVoteHandler? VoteHandler { get; }

        public string ChannelId { get; }

        public Task ConnectOrCreateAsync(bool isReconnection = false);
		public void RemoveInstance();

	}
}
