using MackMultiBot;
using MackMultiBot.Bancho;
using MackMultiBot.Database;
using MackMultiBot.Logging;

Console.Title = "BotLogger";

string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../config.txt");
var config = ConfigReader.ReadConfig(configPath);

BotDatabaseContext.ConnectionString = $"Data Source={config.DatabaseDirectory}/data.db";
Logger.LogFilePath = $"{config.LogDirectory}/Log.txt";

Bot Bot = new(config);
await Bot.StartAsync();

await Task.Delay(-1);