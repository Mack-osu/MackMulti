using NetMQ;
using NetMQ.Sockets;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

Console.Title = "Messenger";


using var pushSocket = new PushSocket();

Console.WriteLine("Connecting...!");
pushSocket.Connect("tcp://localhost:5555");
Console.WriteLine("Connected!");

while (true)
{
	string? msg = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(msg)) continue;

	pushSocket.SendFrame(msg);

}
