using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using MackMultiBot.Bancho.Data;
using MackMultiBot.Bancho.Interfaces;
using MackMultiBot.Logging;
using System.Collections.Concurrent;

namespace MackMultiBot.Bancho
{
	public class MessageHandler(IBanchoConnection banchoConnection) : IMessageHandler
	{
        public bool IsRunning { get; private set; } = false;
        public string ChannelId { get; set; } = "";

        public event Action<IPrivateIrcMessage>? OnMessageReceived;
		public event Action<IPrivateIrcMessage>? OnMessageSent;

        private BlockingCollection<Message> _messageQueue = new(50);

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationToken _cancellationToken => _cancellationTokenSource!.Token;

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
			Logger.Log(LogLevel.Trace, "MessageHandler: Starting message pump");

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
            Logger.Log(LogLevel.Trace, "MessageHandler: Stopping message pump");

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

                if (_cancellationToken.IsCancellationRequested || banchoConnection.BanchoClient == null)
                    break;

                message.Sent = DateTime.UtcNow;

                try
                {
                    await banchoConnection.BanchoClient.SendPrivateMessageAsync(message.Channel, message.Content);
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warn, $"MessageHandler: Failed to send message '{message.Content}' to '{message.Channel}' | Exception: {e}");
                }
            }

            Logger.Log(LogLevel.Info, "MessageHandler: Message pump has stopped");
			IsRunning = false;
		}

        private void BanchoOnPrivateMessageReceived(IPrivateIrcMessage e)
		{
			if (!e.IsDirect && e.Recipient != ChannelId)
				return;

            if (e.IsBanchoBotMessage)
				Logger.Log(LogLevel.Bancho, $"{e.Sender}: {e.Content}");
			else
				Logger.Log(LogLevel.Chat, $"{e.Sender}: {e.Content}");

			OnMessageReceived?.Invoke(e);
        }

        private void BanchoOnPrivateMessageSent(IPrivateIrcMessage e)
		{
			if (!e.IsDirect && e.Recipient != ChannelId)
				return;

			Logger.Log(LogLevel.Chat, $"{e.Sender}: {e.Content}");

			OnMessageSent?.Invoke(e);
        }
    }
}
