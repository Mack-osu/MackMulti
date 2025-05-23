﻿using BanchoSharp.Multiplayer;
using MackMultiBot.Interfaces;
using MackMultiBot.Logging;
using OsuSharp;

namespace MackMultiBot
{
	public class BehaviorEventContext(ILobby lobby)
	{
		public ILobby Lobby { get; init; } = lobby;

		public MultiplayerLobby MultiplayerLobby => Lobby.MultiplayerLobby!;

		/// <summary>
		/// The channel name of the multiplayer lobby, formatted as "#mp_channelId".
		/// </summary>
		public string Channel => Lobby.MultiplayerLobby!.ChannelName;

		public void SendMessage(string contents)
		{
			if (Lobby.MultiplayerLobby == null)
			{
				Logger.Log(LogLevel.Error, "BehaviorEventContext: Attempt to send message while multiplayer lobby is null");
				throw new InvalidOperationException("Attempt to send message while multiplayer lobby is null.");
			}

			Lobby.BanchoConnection.MessageHandler.SendMessage(Channel, contents);
		}

		/// <summary>
		/// Get what string to use when passing a player as a parameter in tournament commands.
		/// This will make sure to prioritize player ID, or use player names if not available.
		/// </summary>
		public string GetPlayerIdentifier(string playerName)
		{
			int? playerId = MultiplayerLobby.Players.FirstOrDefault(x => x.Name == playerName)?.Id;

			return playerId == null ? playerName.ToIrcNameFormat() : $"#{playerId}";
		}

		public async Task<T> UsingApiClient<T>(Func<OsuApiClient, Task<T>> apiCall)
		{
			try
			{
				return await apiCall(Lobby.Bot.OsuApiClient);
			}
			catch (OsuApiException)
			{
				throw;
			}
		}
	}
}