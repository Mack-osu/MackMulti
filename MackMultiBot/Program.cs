using MackMultiBot;
using MackMultiBot.Bancho;
using MackMultiBot.Database;

Console.Title = "BotLogger";

string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
var config = ConfigReader.ReadConfig(configPath);
BotDatabaseContext.ConnectionString = $"Data Source={config.DatabaseDirectory}/data.db";

Console.WriteLine($"Data Source={config.DatabaseDirectory}/data.db");
Console.WriteLine(BotDatabaseContext.ConnectionString);
Bot Bot = new(config);
await Bot.StartAsync();

Console.ReadLine();