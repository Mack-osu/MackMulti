using MackMultiBot;
using MackMultiBot.Bancho.Data;

Bot Bot = new(new BotConfiguration());
await Bot.StartAsync();

Console.ReadLine();