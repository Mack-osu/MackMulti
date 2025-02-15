using MackMultiBot.Bancho;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;

namespace MackMultiBot
{
	public class Bot(BanchoClientConfiguration clientConfiguration)
	{
		NLog.Logger _logger = NLog.LogManager.GetLogger("BotLogger");

		public BanchoConnection BanchoConnection { get; } = new(clientConfiguration);

		// This class will also be used to handle creating and starting lobbies.

		public async Task StartAsync()
		{
			_logger.Info("Bot starting");

			BanchoConnection.OnReady += OnBanchoReady;

			await BanchoConnection.StartAsync();
		}

		private async void OnBanchoReady()
		{
			// Bot ready :)
		}
	}
}
