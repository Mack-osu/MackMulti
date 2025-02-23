using BanchoSharp.Multiplayer;
using MackMultiBot.Data;
using MackMultiBot.Interfaces;
using OsuSharp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Behaviors
{
	public class TestBehavior(BehaviorEventContext context) : IBehavior
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("TestBehaviorLogger");

		[BotEvent(BotEventType.Command, "test")]
		public void TestCommand(CommandContext commandContext)
		{
			commandContext.Reply($"Lobby Players: {string.Join(", ", commandContext.Lobby?.MultiplayerLobby?.Players.Select(x => x.Name + $"[{x.Id}]").ToList()!)}");
			_logger.Info("TestBehavior: Executing TestCommand");
		}

		[BotEvent(BotEventType.Command, "getrecentscore")]
		public async Task GetRecentScores(CommandContext commandContext)
		{
			OsuSharp.Models.Scores.Score? score = (await context.UsingApiClient(async (apiClient) => await apiClient.GetUserScoresAsync(11584934, UserScoreType.Recent, 1, 1, "osu", 1)))?[0];
			commandContext.Reply($"TotalScore: {score?.TotalScore}, IsPass: {score?.IsPass}");
		}
	}
}
