using MackMultiBot;
using MackMultiBot.Bancho;
using MackMultiBot.Database;

Console.Title = "BotLogger";

var config = ConfigReader.ReadConfig("../../../../config.txt");
BotDatabaseContext.ConnectionString = $"Data Source={config.DatabaseDirectory}/data.db";

Console.WriteLine($"Data Source={config.DatabaseDirectory}/data.db");
Console.WriteLine(BotDatabaseContext.ConnectionString);
Bot Bot = new(config);
await Bot.StartAsync();

Console.ReadLine();