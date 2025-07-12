using MackMultiBot.Bancho;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMultiBot
{
	public partial class MessengerForm : Form
	{
		Bot _bot;

		TextBox _inputBox;

		public MessengerForm(Bot bot)
		{
			_bot = bot;

			Text = "Lobby Message Input";
			Width = 800;
			Height = 100;
			FormBorderStyle = FormBorderStyle.FixedSingle;
			ShowInTaskbar = false;
			MaximizeBox = false;
			BackColor = Color.FromArgb(12, 12, 12);

			_inputBox = new TextBox
			{
				Dock = DockStyle.Fill,
				Font = new Font("Consolas", 12),
				BackColor = Color.FromArgb(12, 12, 12),
				ForeColor = Color.White,
				BorderStyle = BorderStyle.None
			};

			_inputBox.KeyDown += InputBox_KeyDown;

			Controls.Add(_inputBox);
		}

		private void InputBox_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				string message = _inputBox.Text.Trim();
				if (!string.IsNullOrEmpty(message))
				{
					Console.WriteLine($"[SENT] {message}");

					if (!string.IsNullOrEmpty(_bot.BanchoConnection.MessageHandler.ChannelId))
						_bot.BanchoConnection.MessageHandler.SendMessage(_bot.BanchoConnection.MessageHandler.ChannelId, message);

					_inputBox.Clear();
				}
			}
		}
	}
}