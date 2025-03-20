using System.Diagnostics;

namespace MackMultiBot.Logging
{
	public static class Logger
	{
		public static string? LogFilePath;

		private static readonly Lock _consoleLock = new();

		public static void Log(LogLevel level, string message)
		{
			string consoleTimestamp = $"{DateTime.Now:HH:mm}";

			lock (_consoleLock)
			{
				ConfigureColorsForTimestamp();
				Console.Write($"[{consoleTimestamp}]");

				ConfigureColorsForLogLevel(level);
				Console.Write($" [{level}] ");

				ConfigureColorsForMessage(level);
				Console.WriteLine(message);
			}

			_ = LogToFileAsync(level, message);
		}

		private static async Task LogToFileAsync(LogLevel level, string message)
		{
			if (LogFilePath == null)
				return;

			string fileTimestamp = $"{DateTime.Now:HH:mm:ss.fff}";
			string fileMessage = $"{fileTimestamp} [{level}] {message}";
			using StreamWriter writer = new(LogFilePath, append: true);
			await writer.WriteLineAsync(fileMessage);
		}

		static void ConfigureColorsForTimestamp()
		{
			Console.ResetColor();
			Console.ForegroundColor = ConsoleColor.DarkGray;
		}

		static void ConfigureColorsForLogLevel(LogLevel? level)
		{
			Console.ResetColor();

			switch (level)
			{
				case LogLevel.Chat:
					Console.ForegroundColor = ConsoleColor.Cyan;
					break;

				case LogLevel.Bancho:
					Console.ForegroundColor = ConsoleColor.DarkBlue;
					break;

				case LogLevel.Trace:
					Console.ForegroundColor = ConsoleColor.Gray;
					break;

				case LogLevel.Info:
					Console.ForegroundColor = ConsoleColor.Green;
					break;

				case LogLevel.Warn:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;

				case LogLevel.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;

				case LogLevel.Fatal:
					Console.ForegroundColor = ConsoleColor.DarkRed;
					break;

				default:
					Console.ResetColor();
					break;
			}
		}

		static void ConfigureColorsForMessage(LogLevel? level)
		{
			Console.ResetColor();

			switch (level)
			{
				case LogLevel.Chat:
					Console.ForegroundColor = ConsoleColor.White;
					break;

				case LogLevel.Bancho:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;

				case LogLevel.Trace:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;

				case LogLevel.Info:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;

				case LogLevel.Warn:
					Console.ForegroundColor = ConsoleColor.White;
					break;

				case LogLevel.Error:
					Console.ForegroundColor = ConsoleColor.White;
					break;

				case LogLevel.Fatal:
					Console.ForegroundColor = ConsoleColor.Red;
					break;

				default:
					Console.ResetColor();
					break;
			}
		}
	}

	public enum LogLevel
	{
		Chat,
		Bancho,

		Trace,
		Info,
		Warn,
		Error,
		Fatal
	}
}
