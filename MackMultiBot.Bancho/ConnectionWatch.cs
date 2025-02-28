using BanchoSharp.Interfaces;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho
{
	public class ConnectionWatch(TcpClient tcpClient, IMessageHandler messageHandler)
	{
		public bool IsRunning { get; private set; } = false;

		public event Action? OnConnectionLost;

		readonly TcpClient _tcpClient = tcpClient;
		readonly IMessageHandler _messageHandler = messageHandler;

		Task? _connectionWatchTask;

		DateTime _lastReceivedMessageTime = DateTime.UtcNow;
		DateTime _lastDummyMessageTime = DateTime.UtcNow;

		CancellationTokenSource? _cancellationTokenSource;
		CancellationToken _cancellationToken => _cancellationTokenSource!.Token;

		public void Start()
		{
			Logger.Log(LogLevel.Trace, "ConnectionState: Starting Connection Watch");
			_cancellationTokenSource = new();
			_connectionWatchTask = Task.Run(ConnectionWatchTask);
		}

		public void Stop()
		{
			Logger.Log(LogLevel.Trace, "ConnectionHandler: Stopping watchdog task...");

			_cancellationTokenSource?.Cancel();

			if (_connectionWatchTask == null || _connectionWatchTask.Status == TaskStatus.RanToCompletion || _connectionWatchTask.Status == TaskStatus.Faulted || _connectionWatchTask.Status == TaskStatus.Canceled)
			{
				Logger.Log(LogLevel.Warn, "ConnectionHandler: Watchdog task is not running during Stop()");

				_connectionWatchTask = null;

				return;
			}

			_connectionWatchTask.Wait();
			_connectionWatchTask = null;
		}

		public async Task ConnectionWatchTask()
		{
			Logger.Log(LogLevel.Info, "ConnectionWatch: Connection Watch started successfully");

			IsRunning = true;

			void OnMessageReceived(IPrivateIrcMessage _)
			{
				_lastReceivedMessageTime = DateTime.UtcNow;
			}

			_messageHandler.OnMessageReceived += OnMessageReceived;

			while (true)
			{
				await Task.Delay(1000);

				if (_cancellationToken.IsCancellationRequested)
					break;

				if (IsConnectionHealthy())
					continue;

				Logger.Log(LogLevel.Error, "ConnectionWatch: Lost Connection");

				_ = Task.Run(() => { OnConnectionLost?.Invoke(); });
			}

			Logger.Log(LogLevel.Info, "ConnectionWatch: Watch has stopped");

			_messageHandler.OnMessageReceived -= OnMessageReceived;

			IsRunning = false;
		}

		bool IsConnectionHealthy()
		{
			if (!IsTcpConnected())
			{
				Logger.Log(LogLevel.Error, "ConnectionWatch: TCP Connection broken");
				return false;
			}

			// Message received in the last 5 minutes
			if ((DateTime.UtcNow - _lastReceivedMessageTime).TotalMinutes < 5)
			{
				return true;
			}

			// No messages sent within the past 5 minutes, send dummy message
			if ((DateTime.UtcNow - _lastDummyMessageTime).TotalMinutes > 5)
			{
				Logger.Log(LogLevel.Warn, "ConnectionWatch: No messages received in the past 5 minutes, sending dummy message");
				_messageHandler.SendMessage("BanchoBot", "dummy");
				_lastDummyMessageTime = DateTime.UtcNow;
			}

			return true;
		}

		// See https://stackoverflow.com/a/6993334
		private bool IsTcpConnected()
		{
			try
			{
				if (_tcpClient != null && _tcpClient.Client.Connected)
				{
					if (!_tcpClient.Client.Poll(0, SelectMode.SelectRead))
					{
						return true;
					}

					byte[] buff = new byte[1];

					return _tcpClient.Client.Receive(buff, SocketFlags.Peek) != 0;
				}
			}
			catch
			{
				return false;
			}

			return false;
		}
	}
}
