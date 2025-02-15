using BanchoSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho.Interfaces
{
	public interface IMessageHandler
	{
        public bool IsRunning { get; }

        public event Action<IPrivateIrcMessage>? OnMessageReceived;
        public event Action<IPrivateIrcMessage>? OnMessageSent;

        public void Start();

        public void Stop();

        public void SendMessage(string channel, string message);
    }
}
