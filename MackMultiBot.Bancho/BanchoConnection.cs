using BanchoSharp;
using BanchoSharp.Interfaces;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;
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
        NLog.Logger _logger = NLog.LogManager.GetLogger("ConnectionLogger");

        public IBanchoClient? BanchoClient { get; private set; }

		public bool IsConnected { get; private set; }

        public IMessageHandler MessageHandler { get; }

        public event Action? OnReady;

        public CancellationToken? ConnectionCancellationToken => _cancellationTokenSource?.Token;

        private CancellationTokenSource? _cancellationTokenSource;
        private readonly BanchoClientConfiguration _banchoConfiguration;

        public BanchoConnection(BanchoClientConfiguration banchoClientConfiguration)
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
            await DisconnectAsync();

            BanchoClient = new BanchoClient( new BanchoClientConfig(new IrcCredentials("[_Mack_]", "d7195ae5"), LogLevel.None, false));

            BanchoClient.OnAuthenticated += BanchoOnAuthenticated;

            _logger.Info("BanchoConnection: Connecting to Bancho");

            try
            {
                await BanchoClient.ConnectAsync();
            }
            catch (Exception e)
            {
                _logger.Error("BanchoConnection: Exception during connection to Bancho, {Exception}", e);
                return;
            }

            _logger.Info("BanchoConnection: Bancho connection terminated");
        }

        private async Task DisconnectAsync()
        {
            if (MessageHandler.IsRunning)
            {
                MessageHandler.Stop();
            }

            if (BanchoClient != null)
            {
                _logger.Info("BanchoConnection: Disconnecting from Bancho");

                await BanchoClient.DisconnectAsync();

                BanchoClient.OnAuthenticated -= BanchoOnAuthenticated;

                BanchoClient?.Dispose();
            }

            BanchoClient = null;
            IsConnected = false;

            _logger.Info("BanchoConnection: Disconnected from Bancho successfully");
        }

        private void BanchoOnAuthenticated()
        {
            _logger.Info("BanchoConnection: Authenticated with Bancho successfully");

            IsConnected = true;

            MessageHandler.Start();

            OnReady?.Invoke();
        }
    }
}
