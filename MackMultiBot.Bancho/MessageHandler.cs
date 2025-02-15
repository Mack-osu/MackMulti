using BanchoSharp.Interfaces;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot.Bancho
{
	internal class MessageHandler(IBanchoConnection banchoConnection) : IMessageHandler
	{
        NLog.Logger _logger = NLog.LogManager.GetLogger("MessageLogger");

        public bool IsRunning { get; private set; } = false;

		public event Action<IPrivateIrcMessage>? OnMessageReceived;
		public event Action<IPrivateIrcMessage>? OnMessageSent;

        private BlockingCollection<Message> _messageQueue = new(50);

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationToken CancellationToken => _cancellationTokenSource!.Token;

        private Task? _messagePumpTask;

        public void SendMessage(string channel, string message)
		{
			_messageQueue.Add(new Message
			{
				Channel = channel,
				Content = message
			});
		}

		public void Start()
		{
			_logger.Trace("MessageHandler: Starting message pump");

			// Empty message queue
			_messageQueue.Dispose();
			_messageQueue = new(20);

			if (banchoConnection.BanchoClient != null)
            {
                banchoConnection.BanchoClient.OnPrivateMessageReceived += BanchoOnPrivateMessageReceived;
                banchoConnection.BanchoClient.OnPrivateMessageSent += BanchoOnPrivateMessageSent;
            }

            _cancellationTokenSource = new();
            _messagePumpTask = Task.Run(MessagePump);
		}

		public void Stop()
        {
            _logger.Trace("MessageHandler: Stopping message pump");

            _cancellationTokenSource?.Cancel();

            if (banchoConnection.BanchoClient != null)
            {
                banchoConnection.BanchoClient.OnPrivateMessageReceived -= BanchoOnPrivateMessageReceived;
                banchoConnection.BanchoClient.OnPrivateMessageSent -= BanchoOnPrivateMessageSent;
            }

            // Sends a message to make sure the message pump is processing something.
            SendMessage("BanchoBot", "message");

            _messagePumpTask?.Wait();
            _messagePumpTask = null;
        }

        async Task MessagePump()
        {
            IsRunning = true;

            while (true)
            {
                var message = _messageQueue.Take();

                if (CancellationToken.IsCancellationRequested || banchoConnection.BanchoClient == null)
                    break;

                message.Sent = DateTime.UtcNow;

                try
                {
                    await banchoConnection.BanchoClient.SendPrivateMessageAsync(message.Channel, message.Content);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "MessageHandler: Failed to send message '{message}' to '{channel}'", message.Content, message.Channel);
                }
            }

            _logger.Trace("MessageHandler: Message pump has stopped");
        }

        private void BanchoOnPrivateMessageReceived(IPrivateIrcMessage e)
        {
            _logger.Info("MessageHandler: [{Recipient}] {Sender}: {Content}", e.Recipient, e.Sender, e.Content);

            OnMessageReceived?.Invoke(e);
        }

        private void BanchoOnPrivateMessageSent(IPrivateIrcMessage e)
        {
            _logger.Info("MessageHandler: [{Recipient}] {Sender}: {Content}", e.Recipient, e.Sender, e.Content);

            OnMessageSent?.Invoke(e);
        }
    }
}
