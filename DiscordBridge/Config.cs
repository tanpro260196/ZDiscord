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

        //4-digits ID for the Bot account
        public string botID;

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

        //Will relay messages from these Bots
        public List<string> WhiteListedBotID;

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
                BotPrefix = "/",
                BotToken = "aaabbbccc",
                botID = "0234",
				ChannelID = 432688657221156873,
				GuildID = 304412012425904129,
				IgnoredDiscordIDs = new List<ulong>() { 101010101010101010 },
                WhiteListedBotID = new List<string>() {"0000", "0234", "0622", "6404", "0769", "4876" },
                LogChannelID = 447479526801408021
            };
		}
	}
}
