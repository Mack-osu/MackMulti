using System.Diagnostics;

namespace MackMultiBot.Logger
{
	public static class Logger
	{
		static string _logFilePath = "E:/Temp/MackMulti/MultiLog";

		public static void Log(string message, LogLevel level)
		{
			// Log to console with limited timestamp and colored output
			string consoleTimestamp = $"{DateTime.Now:HH:mm}";

			ConfigureColorsForTimestamp();
			Console.Write($"[{consoleTimestamp}]");

			ConfigureColorsForLogLevel(level);
			Console.Write($" [{level}] ");

			ConfigureColorsForMessage(level);
			Console.WriteLine(message);

			// Log to file
			string fileTimestamp = $"{DateTime.Now:HH:mm:ss.fff}";
			string fileMessage = $"{fileTimestamp} [{level}] {message}";
			File.AppendAllText(_logFilePath, fileMessage + Environment.NewLine);
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

				case LogLevel.Trace:
					Console.ForegroundColor = ConsoleColor.Gray;
					break;

				case LogLevel.Info:
					Console.ForegroundColor = ConsoleColor.White;
					break;

				case LogLevel.Warn:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;

				case LogLevel.Error:
					Console.ForegroundColor = ConsoleColor.DarkYellow;
					break;

				case LogLevel.Fatal:
					Console.ForegroundColor = ConsoleColor.Red;
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

				case LogLevel.Trace:
					Console.ForegroundColor = ConsoleColor.White;
					break;

				case LogLevel.Info:
					Console.ForegroundColor = ConsoleColor.White;
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

		Trace,
		Info,
		Warn,
		Error,
		Fatal
	}
}
