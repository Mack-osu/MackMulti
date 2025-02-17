using BanchoSharp.Interfaces;
using MackMulti.Database.Databases;
using MackMultiBot.Database;
using MackMultiBot.Database.Entities;
using MackMultiBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public class CommandProcessor(Bot bot)
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("CommandProcessorLogger");

		readonly Dictionary<string, MethodInfo> _commandMethods = new();
		List<ICommand> _commands = [];

		public void Start()
		{
			RegisterCommands();

			bot.BanchoConnection.MessageHandler.OnMessageReceived += OnMessageReceived;
		}

		public void Stop()
		{
			bot.BanchoConnection.MessageHandler.OnMessageReceived -= OnMessageReceived;
			_commandMethods.Clear();
			_commands.Clear();
		}

		private void RegisterCommands()
		{
			_logger.Trace("CommandProcessor: Registering commands...");

			var commands = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(p => typeof(ICommand).IsAssignableFrom(p) && p.IsClass)
				.ToList();

			foreach (var commandType in commands)
			{
				var command = Activator.CreateInstance(commandType);

				if (command == null)
				{
					_logger.Warn("CommandProcessor: Failed to create instance of command {CommandType}", commandType);
					continue;
				}

				_logger.Trace("CommandProcessor: Registered command {CommandType}", commandType);

				_commands.Add((ICommand)command);
			}
		}

		async void OnMessageReceived(IPrivateIrcMessage message)
		{
			if (message.IsBanchoBotMessage || !message.Content.StartsWith("!"))
				return;

			string[] args = message.Content.Split(' ');
			var command = _commands.FirstOrDefault(x => x.Command.ToLower() == args[0][1..].ToLower() || x.Aliases?.Contains(args[0][1..].ToLower()) == true);

			// Command exists?
			if (command == null)
			{
				bot.BanchoConnection.MessageHandler.SendMessage(message.IsDirect ? message.Sender : message.Recipient, $"Could not identify command '{args[0][1..]}'");
				return;
			}

			// Direct message?
			if (message.IsDirect && !command.IsGlobal)
			{
				bot.BanchoConnection.MessageHandler.SendMessage(message.Sender, $"Command '{args[0][1..]}' is not allowed to be executed outside of multiplayer lobby.");
				return;
			}

			// Argument amount?
			if (args.Length - 1 < command.MinimumArguments)
			{
				bot.BanchoConnection.MessageHandler.SendMessage(message.IsDirect ? message.Sender : message.Recipient, $"Insufficient argument amount, command usage: " + command.Usage);
				return;
			}


			// Admin Check
			await using var userDb = new UserDb();
			var user = await userDb.FindOrCreateUser(message.Sender);

			if (command.AdminCommand && !user.IsAdmin)
			{
				bot.BanchoConnection.MessageHandler.SendMessage(message.IsDirect ? message.Sender : message.Recipient, $"Insufficient permissions to execute command '{command.Command}'");
				return;
			}

			var commandContext = new CommandContext(message, args.Skip(1).ToArray(), bot, command, user);

			// Execute the command in the context of a multiplayer lobby
			foreach (var lobby in bot.Lobbies)
			{
				if (lobby.MultiplayerLobby == null || lobby.MultiplayerLobby.ChannelName != message.Recipient || lobby.BehaviorEventProcessor == null)
					continue;

				commandContext.Lobby = lobby;
				commandContext.Player = (BanchoSharp.Multiplayer.MultiplayerPlayer?)lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == message.Sender.ToIrcNameFormat());

				// If the player is in the lobby, retrieve the user from the database with that name instead
				// because the IRC username may change spaces to underscores and crap, and I don't think
				// there's a good way to handle that, since what if the name actually contains an "_"?
				if (commandContext.Player?.Name != null)
				{
					commandContext.User = await userDb.FindOrCreateUser(commandContext.Player!.Name);
				}

				try
				{
					await lobby.BehaviorEventProcessor.OnCommandExecuted(command.Command, commandContext);

				}
				catch (Exception e)
				{
					_logger.Error(e, "CommandProcessor: Error executing command {Command} in lobby {Lobby}", command.Command, lobby.MultiplayerLobby.ChannelName);
				}
			}

			//command?.Execute(new CommandContext(message, args, bot, command, user));
		}

		//public void ExecuteCommand(string command, string[] args)
		//{
		//	if (_commandMethods.TryGetValue(command, out var method))
		//	{
		//		method.Invoke(null, [args]);
		//	}
		//	else
		//	{
		//		Console.WriteLine("CommandProcessor: Command {command} not found.", command);
		//	}
		//}
	}
}
