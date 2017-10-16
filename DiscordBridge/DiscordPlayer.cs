using System;
using System.Collections.Generic;
using TShockAPI;

namespace DiscordBridge
{
	public class DiscordPlayer : TSPlayer, IDisposable
	{
		private List<string> commandOutput;

		public DiscordPlayer(string name) : base(name)
		{
			commandOutput = new List<string>();
		}

		public void Dispose()
		{
		}

		public override void SendErrorMessage(string msg)
		{
			commandOutput.Add(msg);
		}

		public override void SendInfoMessage(string msg)
		{
			commandOutput.Add(msg);
		}

		public override void SendMessage(string msg, byte red, byte green, byte blue)
		{
			commandOutput.Add(msg);
		}

		public override void SendMessage(string msg, Microsoft.Xna.Framework.Color color)
		{
			commandOutput.Add(msg);
		}

		public override void SendSuccessMessage(string msg)
		{
			commandOutput.Add(msg);
		}

		public override void SendWarningMessage(string msg)
		{
			commandOutput.Add(msg);
		}

		public List<string> GetOutput()
		{
			return commandOutput;
		}
	}
}