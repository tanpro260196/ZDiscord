using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TShockAPI;

namespace DiscordBridge
{
	public static class Utils
	{
		private static readonly Regex ColorRegex
		  = new Regex(@"\[c\/\w{3,6}:\w+\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static readonly Regex TextRegex
		  = new Regex(@"\[[a,i,n,g](?:\/\w+)?:([a-zA-Z0-9_ ]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static readonly Regex MentionRegex
			= new Regex(@"<@!{0,1}(\d{18})>", RegexOptions.Compiled);

		private static readonly Regex EmoteRegex
			= new Regex(@"<a?:(\w+):\d+>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static readonly Regex ReverseMentionRegex
			= new Regex(@"@(\S+)", RegexOptions.Compiled);

		public static List<string> ParseParameters(this string input)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			var instr = false;
			for (var i = 0; i < input.Length; i++)
			{
				var c = input[i];

				if (c == '\\' && ++i < input.Length)
				{
					if (input[i] != '"' && input[i] != ' ' && input[i] != '\\')
						sb.Append('\\');
					sb.Append(input[i]);
				}
				else if (c == '"')
				{
					instr = !instr;
					if (!instr)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
					else if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else if (char.IsWhiteSpace(c) && !instr)
				{
					if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else
				{
					sb.Append(c);
				}
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		public static string ParseText(this string raw)
		{
			var result = raw;

			result = ColorRegex.Replace(result, ReplaceString);
			result = SanitizeTag(result);

			result = result.Replace("@everyone", "everyone");

			return result;

			string SanitizeTag(string input)
			{
				string str = TextRegex.Replace(input, m
				  => int.TryParse(m.Groups[1].Value, out var item)
					? $"`{TShock.Utils.GetItemById(item).Name}`"
					: $"`{m.Groups[1].Value}`").Replace("``", "` `");

				str = EmoteRegex.Replace(str, e => $":{e.Groups[1].Value}:");

				return MentionRegex.Replace(str, m => $"@{Discord.GetName(ulong.Parse(m.Groups[1].Value))}");
			}

			string ReplaceString(Capture match)
			  => match.Value.Substring(match.Value.IndexOf(':') + 1).TrimEnd(']');
		}

		public static string ReverseFormat(this string text)
		{
			if (!DiscordMain.Config.AllowPinging)
				return text;
			if (!text.Contains("@"))
				return text;
			return ReverseMentionRegex.Replace(text, e => Discord.GetId(e.Groups[1].Value) != 0 ? $"<@{Discord.GetId(e.Groups[1].Value)}>" : $"@{e.Groups[1].Value}");
		}
	}
}
