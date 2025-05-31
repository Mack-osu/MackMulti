using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Logging;
using NetMQ;
using NetMQ.Sockets;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace MackMultiBot.Bancho
{
	public class Messager(IMessageHandler messageHandler, string channelId)
	{
		public void Start()
		{
			Task.Run(StartMessageServer);
		}

		async Task StartMessageServer()
		{
			using var pullSocket = new PullSocket();

			pullSocket.Bind("tcp://*:5555");
			Logger.Log(LogLevel.Info, "Messager: Listening on port 5555");

			while (true)
			{
				string msg = pullSocket.ReceiveFrameString();

				messageHandler.SendMessage(channelId, msg);
			}
		}

	}
}
