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
	public class Messenger(IMessageHandler messageHandler, string channelId)
	{
		public string ChannelId = channelId;

		public void Start()
		{
			Logger.Log(LogLevel.Trace, "Messenger: Starting Messenger");
			Task.Run(StartMessageSocket);
		}

		Task StartMessageSocket()
		{
			using var pullSocket = new PullSocket();

			pullSocket.Bind("tcp://*:5555");
			Logger.Log(LogLevel.Info, "Messenger: Listening on port 5555");

			while (true)
			{
				string msg = pullSocket.ReceiveFrameString();

				messageHandler.SendMessage(ChannelId, msg);
			}
		}
	}
}
