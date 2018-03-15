using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace DiscordBridge
{
	[ApiVersion(2,1)]
	public class DiscordMain : TerrariaPlugin
	{
		#region Plugin Info
		public override string Author => "Zaicon";
		public override string Description => "Bridges chat between Discord and TShock.";
		public override string Name => "DiscordBridge";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
		#endregion

		public DiscordMain(Main game) : base(game)	{ }

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
			ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
			ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
			PlayerHooks.PlayerChat += OnPlayerChat;
			PlayerHooks.PlayerCommand += OnPlayerCommand;
			PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
			PlayerHooks.PlayerLogout += OnPlayerLogout;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
				ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
				PlayerHooks.PlayerChat -= OnPlayerChat;
				PlayerHooks.PlayerCommand -= OnPlayerCommand;
				PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
				PlayerHooks.PlayerLogout -= OnPlayerLogout;
			}
			base.Dispose(disposing);
		}
		#endregion

		public static Config Config { get; set; }

		#region Hooks
		private void OnInitialize(EventArgs args)
		{
			DB.Initialize();
			Config = Config.Read();
			Discord.InitializeAsync();
		}

		private void OnGreetPlayer(GreetPlayerEventArgs args)
		{
			var player = TShock.Players[args.Who];

			if (player == null || !player.Active || string.IsNullOrWhiteSpace(player.Name))
				return;

			Discord.Send($"```yaml{Environment.NewLine}{player.Name} has joined.```");
			Discord.SendLog($"```yaml{Environment.NewLine}{player.Name} has joined. IP: {player.IP}```");
		}

		private void OnServerChat(ServerChatEventArgs args)
		{
			var player = TShock.Players[args.Who];

			if (player == null || !player.Active || string.IsNullOrWhiteSpace(player.Name))
				return;

			if (args.CommandId._name.Equals("Emote"))
				Discord.Send($"* {player.Name} {args.Text}");
		}

		private void OnServerLeave(LeaveEventArgs args)
		{
			var player = TShock.Players[args.Who];

			if (player == null || !player.Active || string.IsNullOrWhiteSpace(player.Name))
				return;

			Discord.Send($"```yaml{Environment.NewLine}{player.Name} has left.```");
			Discord.SendLog($"```yaml{Environment.NewLine}{player.Name} has left. IP: {player.IP}```");
		}

		private void OnPlayerChat(PlayerChatEventArgs args)
		{
			if (!args.Player.mute)
				Discord.Send($"{args.TShockFormattedText.ReverseFormat()}");
		}

		private void OnPlayerCommand(PlayerCommandEventArgs args)
		{
			if (!args.CommandName.Equals("password") && !args.CommandName.Equals("login") && !args.CommandName.Equals("logout") && !args.CommandName.Equals("me") && !args.CommandName.Equals("register"))
				Discord.SendLog($"```yaml{Environment.NewLine}{args.Player.Name} executed: {args.CommandPrefix}{args.CommandText}```");

			if (args.CommandName.Equals("me"))
				Discord.Send($"* {args.Player.Name} {args.CommandText.Substring(3)}");
		}

		private void OnPlayerPostLogin(PlayerPostLoginEventArgs args)
		{
			Discord.SendLog($"```yaml{Environment.NewLine}Player {args.Player.User.Name} has logged in as user {args.Player.Name} of group [{args.Player.Group.Name}]```");
		}

		private void OnPlayerLogout(PlayerLogoutEventArgs args)
		{
			Discord.SendLog($"```yaml{Environment.NewLine}{args.Player.Name} has logged out.```");
		}
		#endregion
	}
}
