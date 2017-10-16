using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;

namespace DiscordBridge
{
	public static class DB
	{
		private static IDbConnection db;

		public static void Initialize()
		{
			//Connect to database depending on config
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					var dbHost = TShock.Config.MySqlHost.Split(':');

					db = new MySqlConnection($"Server={dbHost[0]};" +
												$"Port={(dbHost.Length == 1 ? "3306" : dbHost[1])};" +
												$"Database={TShock.Config.MySqlDbName};" +
												$"Uid={TShock.Config.MySqlUsername};" +
												$"Pwd={TShock.Config.MySqlPassword};");

					break;

				case "sqlite":
					db = new SqliteConnection($"uri=file://{Path.Combine(TShock.SavePath, "Discord.sqlite")},Version=3");
					break;

				default:
					throw new ArgumentException("Invalid storage type in config.json!");
			}

			//Ensure we have a supported table to read/write to
			// [discord]
			// discordid BIGINT Primary Unique
			// tshockid INT
			SqlTableCreator creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
			creator.EnsureTableStructure(new SqlTable("discord",
				new SqlColumn("discordid", MySqlDbType.Int64) { Primary = true, Unique = true },
				new SqlColumn("tshockid", MySqlDbType.Int32)));
		}

		public static int GetTShockID(ulong discordid)
		{
			string query = $"SELECT * FROM discord WHERE discordid = {discordid};";
			using (var result = db.QueryReader(query))
			{
				if (result.Read())
				{
					return result.Get<int>("tshockid");
				}
			}
			return -1;
		}

		public static void AddTShockUser(ulong discordid, int tshockid)
		{
			string query = $"INSERT INTO discord (discordid, tshockid) VALUES ({discordid}, {tshockid});";
			db.Query(query);
		}

		public static void RemoveTShockUser(ulong discordid)
		{
			string query = $"DELETE FROM discord WHERE discordid = {discordid};";
			db.Query(query);
		}
	}
}
