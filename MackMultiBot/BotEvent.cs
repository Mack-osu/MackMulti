using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	[AttributeUsage(AttributeTargets.Method)]
	public class BotEvent(BotEventType type, string? optionalArgument = null) : Attribute
	{
		public BotEventType Type { get; init; } = type;
		public string? OptionalArgument { get; init; } = optionalArgument;
	}

	public enum BotEventType
	{
		// Bot Events
		Initialize,
		Command,
		BehaviorEvent,

		// Bancho Events
		MessageReceived,
		BanchoBotMessageReceived,
		MatchStarted,
		MatchFinished,
		MatchAborted,
		PlayerJoined,
		PlayerDisconnected,
		HostChanged,
		HostChangingMap,
		MapChanged,
		SettingsUpdated,
		AllPlayersReady
	}
}
