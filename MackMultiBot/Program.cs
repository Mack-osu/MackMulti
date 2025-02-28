using MackMultiBot;
using MackMultiBot.Bancho.Data;

Console.Title = "BotLogger";

Bot Bot = new(new BotConfiguration());
await Bot.StartAsync();

Console.ReadLine();