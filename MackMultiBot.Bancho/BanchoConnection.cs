using BanchoSharp;
using BanchoSharp.Interfaces;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho
{
	public class BanchoConnection : IBanchoConnection
    {
        public IBanchoClient? BanchoClient { get; private set; }

		public bool IsConnected { get; private set; }

        public IMessageHandler MessageHandler { get; }

        public event Action? OnReady;

        public CancellationToken? ConnectionCancellationToken => _cancellationTokenSource?.Token;

        private CancellationTokenSource? _cancellationTokenSource;
        private readonly BotConfiguration _banchoConfiguration;

        public BanchoConnection(BotConfiguration banchoClientConfiguration)
        {
            _banchoConfiguration = banchoClientConfiguration;

            MessageHandler = new MessageHandler(this);
        }

        public Task StartAsync()
		{
            _ = Task.Run(ConnectAsync);

            return Task.CompletedTask;
        }

		public async Task StopAsync()
        {
            await DisconnectAsync();
        }

        private async Task ConnectAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Make sure we're disconnected before reconnecting
            if (IsConnected)
                await DisconnectAsync();

			BanchoClient = new BanchoClient( new BanchoClientConfig(new IrcCredentials(_banchoConfiguration.Username, _banchoConfiguration.Password), BanchoSharp.LogLevel.None, false));

            BanchoClient.OnAuthenticated += BanchoOnAuthenticated;

            Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Connecting to Bancho");

            try
            {
                await BanchoClient.ConnectAsync();
            }
            catch (Exception e)
            {
				Logging.Logger.Log(Logging.LogLevel.Error, $"BanchoConnection: Exception during connection to Bancho, {e}");
                return;
            }

			Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Bancho connection terminated");
        }

        private async Task DisconnectAsync()
        {
            if (MessageHandler.IsRunning)
            {
                MessageHandler.Stop();
            }

            if (BanchoClient != null)
            {
				Logging.Logger.Log(Logging.LogLevel.Trace, "BanchoConnection: Disconnecting from Bancho");

                await BanchoClient.DisconnectAsync();

                BanchoClient.OnAuthenticated -= BanchoOnAuthenticated;

                BanchoClient?.Dispose();
            }

            BanchoClient = null;
            IsConnected = false;

			Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Disconnected from Bancho successfully");
        }

        private void BanchoOnAuthenticated()
        {
			Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Authenticated with Bancho successfully");

            IsConnected = true;

            MessageHandler.Start();

            OnReady?.Invoke();
        }
    }
}
