using MackMultiBot.Bancho.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho
{
	public class ConnectionState(TcpClient tcpClient, IMessageHandler messageHandler)
	{
		private readonly TcpClient _tcpClient = tcpClient;
		private readonly IMessageHandler _messageHandler = messageHandler;
	}
}
