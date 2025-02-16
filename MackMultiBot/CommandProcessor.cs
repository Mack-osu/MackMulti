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

		public void RegisterHandlers()
		{
			_logger.Trace("CommandProcessor: Registering handlers...");
			var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(p => typeof(IHandler).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

			foreach (var type in handlerTypes)
			{
				_logger.Info("CommandProcessor: Creating Handler of type {handler}", type.Name);
				RegisterCommandMethod((IHandler)Activator.CreateInstance(type));
			}
		}

		private void RegisterCommandMethod(IHandler handler)
		{
			_logger.Trace("CommandProcessor: Registering methods...");
			var methods = handler.GetType().GetMethods();
			foreach (var method in methods)
			{
				var attribute = method.GetCustomAttribute<CommandAttribute>();
				if (attribute != null)
				{
					_logger.Info("CommandProcessor: Registering Command Method: {method}", method.Name);
					_commandMethods[attribute.Command] = method;
				}
			}
		}

		void RegisterCommands()
		{
			_logger.Trace("CommandProcessor: Registering commands...");

			var commands = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(y => typeof(ICommand).IsAssignableFrom(y) && y.IsClass)
				.ToList();

			foreach (var commandType in commands)
			{
				var command = Activator.CreateInstance(commandType);

				if (command == null)
				{
					Console.WriteLine($"CommandProcessor: Failed to create instance of command {commandType}");
					continue;
				}

				Console.WriteLine($"CommandProcessor: Registered command {commandType}");

				_commands.Add((ICommand)command);
			}
		}

		public void Start()
		{
			RegisterHandlers();
			RegisterCommands();
			bot.BanchoConnection.MessageHandler.OnMessageReceived += OnMessageReceived;
		}

		public void Stop()
		{
			bot.BanchoConnection.MessageHandler.OnMessageReceived -= OnMessageReceived;
			_commandMethods.Clear();
			_commands.Clear();
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

			ExecuteCommand(command.Command.ToLower(), args);

			//command?.Execute(new CommandContext(message, args, bot, command, user));
		}

		public void ExecuteCommand(string command, string[] args)
		{
			if (_commandMethods.TryGetValue(command, out var method))
			{
				method.Invoke(null, [args]);
			}
			else
			{
				Console.WriteLine("CommandProcessor: Command {command} not found.", command);
			}
		}
	}
}
