using MackMultiBot;
using MackMultiBot.Bancho.Data;

var logger = NLog.LogManager.GetLogger("ProgramLogger");
logger.Info("Program: Program Started");

Bot Bot = new(new BanchoClientConfiguration());
await Bot.StartAsync();

Console.ReadLine();
NLog.LogManager.Shutdown();