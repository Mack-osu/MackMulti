using MackMultiBot;
using MackMultiBot.Bancho.Data;

var logger = NLog.LogManager.GetLogger("Logger");
logger.Info("App Started");

Bot Bot = new(new BanchoClientConfiguration());
await Bot.StartAsync();

Console.ReadLine();
NLog.LogManager.Shutdown();