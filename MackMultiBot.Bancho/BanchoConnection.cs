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
using System.Net.Sockets;

namespace MackMultiBot.Bancho
{
	public class BanchoConnection : IBanchoConnection
    {
        public IBanchoClient? BanchoClient { get; private set; }

		public bool IsConnected { get; private set; }

        public IMessageHandler MessageHandler { get; }

        public event Action? OnReady;

        public CancellationToken? ConnectionCancellationToken => _cancellationTokenSource?.Token;

        CancellationTokenSource? _cancellationTokenSource;
        readonly BotConfiguration _banchoConfiguration;

        ConnectionWatch? _connectionWatch;

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

        async Task ConnectAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Make sure we're disconnected before reconnecting
            if (IsConnected)
                await DisconnectAsync();

            BanchoClient?.Dispose();

			BanchoClient = new BanchoClient( new BanchoClientConfig(new IrcCredentials(_banchoConfiguration.IrcUsername, _banchoConfiguration.IrcPassword), BanchoSharp.LogLevel.None, false));

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
			if (_connectionWatch?.IsRunning == true)
			{
				_connectionWatch.OnConnectionLost -= OnConnectionLost;
				_connectionWatch.Stop();
				_connectionWatch = null;
			}

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
			if (BanchoClient?.TcpClient == null)
				return;

			Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Authenticated with Bancho successfully");

            IsConnected = true;

            _connectionWatch = new ConnectionWatch(BanchoClient.TcpClient, MessageHandler);
			_connectionWatch.OnConnectionLost += OnConnectionLost;
            _connectionWatch.Start();

			MessageHandler.Start();

            OnReady?.Invoke();
		}

		private async void OnConnectionLost()
		{
            
			IsConnected = false;

			_cancellationTokenSource?.Cancel();

			Logging.Logger.Log(Logging.LogLevel.Error, $"BanchoConnection: Connection lost, attempting to reconnect in {10} seconds...");

			await Task.Delay(10 * 1000);

			int connectionAttempts = 0;
			while (connectionAttempts < 5)
			{
				Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Attempting to reconnect...");

				_ = Task.Run(ConnectAsync);

				await Task.Delay(10000);

				if (IsConnected)
				{
					Logging.Logger.Log(Logging.LogLevel.Info, "BanchoConnection: Reconnected successfully");

					return;
				}

				Logging.Logger.Log(Logging.LogLevel.Error, $"BanchoConnection: Reconnection failed, retrying in {10} seconds...");

				await Task.Delay(10 * 1000);

				connectionAttempts++;
			}

			Logging.Logger.Log(Logging.LogLevel.Fatal, $"BanchoConnection: Failed to reconnect after {5} attempts, shutting down...");

			DisconnectAsync().Wait(TimeSpan.FromSeconds(30));
		}
	}
}
