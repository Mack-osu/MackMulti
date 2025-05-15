using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Logging;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace MackMultiBot.Bancho
{
	public class Messager(IMessageHandler messageHandler, string channelId)
	{
		CancellationToken? _cancellationToken;

		CancellationTokenSource? _cancellationTokenSource;

		public void Start()
		{
			// Commenting this for now as it does not work as intended.

			//Logger.Log(LogLevel.Trace, "Messager: Starting");

			//_cancellationTokenSource = new();
			//_cancellationToken = _cancellationTokenSource.Token;
			//Task.Run(() => StartPipeServer());
		}

		public void Stop()
		{
			//Logger.Log(LogLevel.Trace, "Messager: stopping messager");
			//_cancellationTokenSource?.Cancel();
		}

		async Task StartPipeServer()
		{
			while (_cancellationToken?.IsCancellationRequested == false)
			{
				using var server = new NamedPipeServerStream("MessagePipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

				try
				{
					Logger.Log(LogLevel.Trace, "Messager: Waiting for client connection...");
					await server.WaitForConnectionAsync(_cancellationToken.Value);

					Logger.Log(LogLevel.Trace, "Messager: Client connected.");

					using var reader = new StreamReader(server);
					string? message;

					while ((message = await reader.ReadLineAsync()) != null)
					{
						messageHandler.SendMessage(channelId, message);
					}
				}
				catch (OperationCanceledException)
				{
					Logger.Log(LogLevel.Info, "Messager: Cancelled pipe server.");
					break;
				}
				catch (IOException ioEx)
				{
					Logger.Log(LogLevel.Error, $"Messager: Pipe disconnected or read failed - {ioEx}");
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Error, $"Messager: Unexpected error - {ex}");
				}

				// Delay before accepting next client, only if not cancelled
				if (!_cancellationToken?.IsCancellationRequested == true)
				{
					await Task.Delay(500);
					Logger.Log(LogLevel.Trace, "Messager: Ready for next client.");
				}

				server.Disconnect();
				server.Dispose();
			}
		}

	}
}
