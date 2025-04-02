
using MackMultiBot.Bancho.Data;
using MackMultiBot.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MackMultiBot.Bancho
{
	public class ConfigReader
	{
		public static BotConfiguration ReadConfig(string filePath)
		{
			var config = new BotConfiguration();
			var lines = File.ReadAllLines(filePath);

			// Default values
			config.DatabaseDirectory = Path.GetDirectoryName(filePath)!;
			config.LogDirectory = Path.GetDirectoryName(filePath)!;

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || !line.Contains('=') || line.StartsWith("//"))
					continue;

				var parts = line.Split('=');
				if (parts.Length != 2)
					continue;

				var key = parts[0].Trim();
				var value = parts[1].Trim();

				switch (key)
				{
					case "IrcUsername":
						config.IrcUsername = value;
						break;
					case "IrcPassword":
						config.IrcPassword = value;
						break;
					case "ApiClientId":
						config.ApiClientId = value;
						break;
					case "ApiClientSecret":
						config.ApiClientSecret = value;
						break;
					case "DatabaseDirectory":
						if (string.IsNullOrWhiteSpace(value))
							break;
						config.DatabaseDirectory = value;
						break;
					case "LogDirectory":
						if (string.IsNullOrWhiteSpace(value))
							break;
						config.DatabaseDirectory = value;
						break;
					case "LobbyName":
						config.LobbyName = value;
						break;
					case "TeamMode":
						config.TeamMode = int.Parse(value);
						break;
					case "ScoreMode":
						config.ScoreMode = int.Parse(value);
						break;
					case "Mods":
						if (string.IsNullOrWhiteSpace(value))
						{
							config.Mods = ["None"];
							break;
						}

						config.Mods = value.Split(", ");
						break;
					case "Size":
						config.Size = int.Parse(value);
						break;
					case "Password":
						if (string.IsNullOrWhiteSpace(value))
						{
							config.Password = string.Empty;
							break;
						}
						config.Password = value;
						break;
					case "LimitDifficulty":
						config.LimitDifficulty = bool.Parse(value);
						break;
					case "LimitMapLength":
						config.LimitMapLength = bool.Parse(value);
						break;
					case "MinimumDifficulty":
						config.MinimumDifficulty = float.Parse(value, CultureInfo.InvariantCulture);
						break;
					case "MaximumDifficulty":
						config.MaximumDifficulty = float.Parse(value, CultureInfo.InvariantCulture);
						break;
					case "DifficultyMargin":
						config.DifficultyMargin = float.Parse(value, CultureInfo.InvariantCulture);
						break;
					case "MinimumMapLength":
						config.MinimumMapLength = int.Parse(value);
						break;
					case "MaximumMapLength":
						config.MaximumMapLength = int.Parse(value);
						break;
					default:
						Logger.Log(LogLevel.Warn, $"Unknown key: {key}");
						break;
				}
			}

			return config;
		}
	}

}
