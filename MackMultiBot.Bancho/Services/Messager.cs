using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Logging;
using System.IO.Pipes;

namespace MackMultiBot.Bancho
{
	public class Messager(IMessageHandler messageHandler, string channelId)
	{
		private NamedPipeServerStream? _pipeServer;

		public void Start()
		{
			Task.Run(() => StartPipeServer());
		}

		// Starts the named pipe server to listen for input from another console
		private void StartPipeServer()
		{
			_pipeServer = new NamedPipeServerStream("MessagePipe", PipeDirection.In);

			Logger.Log(LogLevel.Info, "Messager: Waiting for messages from the secondary console...");
			_pipeServer.WaitForConnection();

			using var reader = new StreamReader(_pipeServer);

			while (true)
			{
				string? message = reader.ReadLine();

				if (message != null)
				{
					messageHandler.SendMessage(channelId, message);
				}
			}
		}
	}
}
