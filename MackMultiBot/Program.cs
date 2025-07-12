using MackMultiBot;
using MackMultiBot.Bancho;
using MackMultiBot.Database;
using MackMultiBot.Logging;
using System.Windows.Forms;

Console.Title = "BotLogger";

Logger.Log(LogLevel.MackMulti, "--------------------------------------------------------------------------------", ConsoleColor.White);
Logger.Log(LogLevel.MackMulti, "    __  ___              __    __  ___        __ __   _ ", ConsoleColor.Green);
Logger.Log(LogLevel.MackMulti, "   /  |/  /____ _ _____ / /__ /  |/  /__  __ / // /_ (_)", ConsoleColor.Green);
Logger.Log(LogLevel.MackMulti, "  / /|_/ // __ `// ___// //_// /|_/ // / / // // __// / ", ConsoleColor.Green);
Logger.Log(LogLevel.MackMulti, " / /  / // /_/ // /__ / ,<  / /  / // /_/ // // /_ / /", ConsoleColor.Green);
Logger.Log(LogLevel.MackMulti, @"/_/  /_/ \__,_/ \___//_/|_|/_/  /_/ \__,_//_/ \__//_/", ConsoleColor.Green);
Logger.Log(LogLevel.MackMulti, "");
Logger.Log(LogLevel.MackMulti, "--------------------------------------------------------------------------------", ConsoleColor.White);
Logger.Log(LogLevel.MackMulti, "Bot Version: v1.0", ConsoleColor.DarkCyan);
Logger.Log(LogLevel.MackMulti, "Report any issues you encounter to me through discord @mackosu", ConsoleColor.DarkCyan);
Logger.Log(LogLevel.MackMulti, "--------------------------------------------------------------------------------", ConsoleColor.White);

string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../config.txt");
Logger.Log(LogLevel.MackMulti, "Reading config file...", ConsoleColor.White);
var config = ConfigReader.ReadConfig(configPath);

Logger.Log(LogLevel.MackMulti, "Initializing database...", ConsoleColor.White);
BotDatabaseContext.ConnectionString = $"Data Source={config.DatabaseDirectory}/data.db";

Logger.Log(LogLevel.MackMulti, "Initializing log file...", ConsoleColor.White);
Logger.LogFilePath = $"{config.LogDirectory}/Log.txt";

Logger.Log(LogLevel.MackMulti, "Starting Bot...", ConsoleColor.White);
Logger.Log(LogLevel.MackMulti, "--------------------------------------------------------------------------------", ConsoleColor.White);
Bot Bot = new(config);
await Bot.StartAsync();

Application.Run(new MessengerForm(Bot));

await Task.Delay(-1);