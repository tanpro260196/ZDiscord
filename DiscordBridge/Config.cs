using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TShockAPI;

namespace DiscordBridge
{
	public class Config
	{
		//Token used to connect to the bot
		public string BotToken;
		
		//Prefix used for bot commands
		public string BotPrefix;

		//Guild ID for the bot to watch for commands
		public ulong GuildID;

		//Channel ID for the bot to send in-game messages
		public ulong ChannelID;

		//Channel ID for the bot to send log messages - optional
		public ulong LogChannelID;

		//Allows pinging from in-game users
		public bool AllowPinging;

		//Will not relay messages from these IDs
		public List<ulong> IgnoredDiscordIDs;

		public static Config Read()
		{
			string configPath = Path.Combine(TShock.SavePath, "discordConfig.json");
			if (!File.Exists(configPath))
			{
				TShock.Log.ConsoleError("No Discord config found. Creating new one and disabling Discord bridge.");
				File.WriteAllText(configPath, JsonConvert.SerializeObject(Default(), Formatting.Indented));
				Discord.Enabled = false;
				return Default();
			}
			try
			{
				return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
			}
			catch
			{
				TShock.Log.ConsoleError("Discord config was invalid. Disabling discord connection.");
				Discord.Enabled = false;
				return Default();
			}
		}

		private static Config Default()
		{
			return new Config()
			{
				AllowPinging = false,
				BotPrefix = "!",
				BotToken = "aaabbbccc",
				ChannelID = 101010101010101010,
				GuildID = 101010101010101010,
				IgnoredDiscordIDs = new List<ulong>() { 101010101010101010 },
				LogChannelID = 101010101010101010
			};
		}
	}
}
