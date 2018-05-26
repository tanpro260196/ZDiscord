using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Utilities;
using TShockAPI;

namespace DiscordBridge
{
	public static class Discord
	{
		private static DiscordSocketClient client;

		//Enabled is for force-disconnection, mainly if provided config values are not valid.
		public static bool Enabled = true;
		//AwaitingConnection is so we don't try to send anything before the bot has properly connected to the guild.
		public static bool AwaitingConnection = true;
		//LogEnabled is if they want to send "log" messages to Discord.
		public static bool LogEnabled = false;

		public static async void InitializeAsync()
		{
			if (!Enabled)
				return;

			client = new DiscordSocketClient();
			client.GuildAvailable += OnGuildAvailable;
			client.MessageReceived += OnMessageReceived;
			try
			{
				await client.LoginAsync(TokenType.Bot, DiscordMain.Config.BotToken);
			}
			catch
			{
				Enabled = false;
				TShock.Log.ConsoleError("Invalid Discord bot token. Disabled Discord bridge.");
				return;
			}
			await client.StartAsync();
		}

		#region Hooks
		private static Task OnGuildAvailable(SocketGuild guild)
		{
			if (!Enabled)
				return Task.CompletedTask;
			//Discord.NET likes to reconnect periodically, so we don't want to do this every time it reconnects.
			if (!AwaitingConnection)
				return Task.CompletedTask;

			if (DiscordMain.Config.GuildID == guild.Id)
			{
				AwaitingConnection = false;

				//Validate channel IDs
				bool hasMainChannel = guild.Channels.Any(e => e.Id == DiscordMain.Config.ChannelID);
				if (!hasMainChannel)
				{
					TShock.Log.ConsoleError("Discord chat channel not found. Disabling Discord bridge.");
					Enabled = false;
					return Task.CompletedTask;
				}
				bool hasLogChannel = guild.Channels.Any(e => e.Id == DiscordMain.Config.LogChannelID);
				if (hasLogChannel)
					LogEnabled = true;

				TShock.Log.Info("Connected to Discord!");
                		Discord.client.SetGameAsync("Terraria", "https://terraria.org");
				Send($"Server Online.");
			}

			return Task.CompletedTask;
		}

		private static Task OnMessageReceived(SocketMessage args)
		{
			if (!Enabled || AwaitingConnection)
				return Task.CompletedTask;
			if (string.IsNullOrWhiteSpace(args.Content))
				return Task.CompletedTask;
			if (args.Author.IsBot && args.Author.Discriminator != "6404" && args.Author.Discriminator != "0000")
				return Task.CompletedTask;
			if (DiscordMain.Config.IgnoredDiscordIDs.Contains(args.Author.Id))
				return Task.CompletedTask;

			//Metadata part 1
			bool isDirectMessage = args.Channel is IDMChannel;
            bool isCommand = isDirectMessage || args.Content.StartsWith(DiscordMain.Config.BotPrefix);
            bool isInMainChannel = args.Channel.Id == DiscordMain.Config.ChannelID;
            

			//Metadata part 2
			int tShockUserId = DB.GetTShockID(args.Author.Id);
			var guild = client.GetGuild(DiscordMain.Config.GuildID);
			var discordUser = guild.GetUser(args.Author.Id);
			var tshockUser = TShock.Users.GetUserByID(tShockUserId);
			var tshockGroup = TShock.Groups.GetGroupByName(tshockUser != null ? tshockUser.Group : TShock.Config.DefaultGuestGroupName);

            //Ignore messages that aren't commands or from main chat channel
            if (!isCommand && !isInMainChannel)
                return Task.CompletedTask;
            

            // Broadcast chat messages
            if ((!isCommand) && !args.Author.IsBot)
            {
                TShock.Utils.Broadcast($"[Discord] {tshockGroup.Prefix}{GetName(args.Author.Id)}: {args.Content.ParseText()}", tshockGroup.R, tshockGroup.G, tshockGroup.B);
                return Task.CompletedTask;
            }
            if ((!isCommand) && args.Author.IsBot)
            {
                TShock.Utils.Broadcast($"[Messenger] {args.Author.Username}: {args.Content.ParseText().Replace("*", string.Empty)}", 0, 132, 255);
                return Task.CompletedTask;
            }

            //If someone DMs bot without being in guild
            if (discordUser == null)
                return Task.CompletedTask;




            string commandText = args.Content.StartsWith(DiscordMain.Config.BotPrefix) ? args.Content.Substring(DiscordMain.Config.BotPrefix.Length).ParseText() : args.Content.ParseText();
			List<string> commandParameters = commandText.ParseParameters();

			//Override certain commands for Discord use
			switch (commandParameters[0].ToLower())
			{
				case "login":
					if (isInMainChannel)
					{
						//Try to delete message, if possible.
						try
						{
							args.DeleteAsync();
						}
						catch { }
						args.Channel.SendMessageAsync("```You can only login via Direct Message with me!```");
						return Task.CompletedTask;
					}
					if (tshockUser != null)
					{
						args.Channel.SendMessageAsync("```You are already logged in!```");
						return Task.CompletedTask;
					}
					if (commandParameters.Count != 3)
					{
						args.Channel.SendMessageAsync("```Invalid syntax: login \"username\" <password>```");
						return Task.CompletedTask;
					}
					var newTshockUser = TShock.Users.GetUserByName(commandParameters[1]);
					if (newTshockUser == null || !newTshockUser.VerifyPassword(commandParameters[2]))
					{
						args.Channel.SendMessageAsync("```Invalid username or password.```");
						return Task.CompletedTask;
					}
					DB.AddTShockUser(args.Author.Id, newTshockUser.ID);
					args.Channel.SendMessageAsync("```Login successful!```");
					break;
				case "logout":
					if (tshockUser == null)
					{
						args.Channel.SendMessageAsync("```You are not logged in!```");
						return Task.CompletedTask;
					}
					DB.RemoveTShockUser(args.Author.Id);
					args.Channel.SendMessageAsync("```Logout successful!```");
					break;
				case "who":
				case "online":
				case "playing":
					args.Channel.SendMessageAsync($"```Active Players ({TShock.Utils.ActivePlayers()}/{TShock.Config.MaxSlots}):\n{string.Join(", ", TShock.Players.Where(e => e != null && e.Active).Select(e => e.Name))}```");
					break;
				case "me":
					if (commandParameters.Count > 0)
						TShock.Utils.Broadcast($"* {GetName(args.Author.Id)} {commandText.Substring(3)}", 205, 133, 63);
					break;
				default:
					using (var player = new DiscordPlayer(GetName(args.Author.Id)))
					{
						player.User = tshockUser;
						player.Group = tshockGroup;

						if (!player.HasPermission("discord.commands"))
						{
							args.Channel.SendMessageAsync("You do not have permission to use commands on Discord. Login with your TEARaria Account with `/login`");
							return Task.CompletedTask;
						}

						var commands = Commands.ChatCommands.Where(c => c.HasAlias(commandParameters[0].ToLower()));
						if (commands.Count() != 0)
						{
							if (Main.rand == null)
								Main.rand = new UnifiedRandom();

							foreach (var command in commands)
								if (!command.CanRun(player))
								{
									args.Channel.SendMessageAsync("```You do not have access to this command.```");
								}
								else if (!command.AllowServer)
								{
									args.Channel.SendMessageAsync("```This command is only available in-game.```");
								}
								else
								{
									command.Run(commandText, player, commandParameters.GetRange(1, commandParameters.Count - 1));
									if (player.GetOutput().Count == 0)
										return Task.CompletedTask;

									args.Channel.SendMessageAsync($"```css{Environment.NewLine}{string.Join("\n", player.GetOutput())}```");
								}
						}
						else
						{
							args.Channel.SendMessageAsync("```Invalid command.```");
						}
					}
					break;
			}

			return Task.CompletedTask;
		}
		#endregion

		#region Sending Messages
		public static void Send(string message)
		{
			if (Enabled && !AwaitingConnection)
				client.GetGuild(DiscordMain.Config.GuildID).GetTextChannel(DiscordMain.Config.ChannelID).SendMessageAsync(message);
		}

		public static void SendLog(string message)
		{
			if (Enabled && !AwaitingConnection && LogEnabled)
				client.GetGuild(DiscordMain.Config.GuildID).GetTextChannel(DiscordMain.Config.LogChannelID).SendMessageAsync(message);
		}
		#endregion

		#region Utils
		public static string GetName(ulong discordid)
		{
			var user = client.GetGuild(DiscordMain.Config.GuildID).GetUser(discordid);
			if (user == null)
				return "UnknownUser";
			return user.Nickname ?? user.Username;
		}

		public static ulong GetId(string name)
		{
			var nicknameUser = client.GetGuild(DiscordMain.Config.GuildID).Users.FirstOrDefault(e => e.Nickname != null && e.Nickname.Equals(name, StringComparison.CurrentCultureIgnoreCase));

			if (nicknameUser != null)
			{
				return nicknameUser.Id;
			}
			else
			{
				var usernameUser = client.GetGuild(DiscordMain.Config.GuildID).Users.FirstOrDefault(e => e.Username.Equals(name, StringComparison.CurrentCultureIgnoreCase));
				if (usernameUser == null)
					return 0;
				else
					return usernameUser.Id;
			}
		}
		#endregion
	}
}
