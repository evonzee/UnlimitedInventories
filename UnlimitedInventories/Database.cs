using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using Terraria;
using Terraria.Localization;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace UnlimitedInventories
{
	/// <summary>
	/// Represents the UnlimitedInventories database manager.
	/// </summary>
	public sealed class Database : IDisposable
	{
		private readonly Dictionary<int, PlayerInfo> _cache = new Dictionary<int, PlayerInfo>();
		private IDbConnection _db;

		/// <summary>
		/// Connects the database.
		/// </summary>
		public void ConnectDatabase()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					_db = new MySqlConnection
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
					_db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;

			}

			SqlTableCreator sqlcreator = new SqlTableCreator(_db, _db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("UnlimitedInventories",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("Name", MySqlDbType.Text),
				new SqlColumn("Inventory", MySqlDbType.Text)));

			LoadDatabase();
		}

		/// <summary>
		/// Disposes the database manager.
		/// </summary>
		public void Dispose()
		{
			_db.Dispose();
		}

		private void LoadDatabase()
		{
			using (var reader = _db.QueryReader("SELECT * FROM UnlimitedInventories"))
			{
				while (reader.Read())
				{
					var userId = reader.Get<int>("UserID");
					var name = reader.Get<string>("Name");
					var inventory = reader.Get<string>("Inventory").Split('~').Select(NetItem.Parse).ToArray();

					if (_cache.ContainsKey(userId))
					{
						_cache[userId].Inventories.Add(name, inventory);
					}
					else
					{
						_cache.Add(userId, new PlayerInfo(userId, new Dictionary<string, NetItem[]> {[name] = inventory}));
					}
				}
			}
		}

		private static NetItem[] CreateInventory(TSPlayer player)
		{
			int index;
			var playerInventory = new NetItem[NetItem.MaxInventory];

			var inventory = player.TPlayer.inventory;
			var armor = player.TPlayer.armor;
			var dye = player.TPlayer.dye;
			var miscEquips = player.TPlayer.miscEquips;
			var miscDyes = player.TPlayer.miscDyes;
			var piggy = player.TPlayer.bank.item;
			var safe = player.TPlayer.bank2.item;
			var trash = player.TPlayer.trashItem;
			var forge = player.TPlayer.bank3.item;

			for (var i = 0; i < NetItem.MaxInventory; i++)
			{
				if (i < NetItem.InventoryIndex.Item2)
				{
					playerInventory[i] = (NetItem)inventory[i];
				}
				else if (i < NetItem.ArmorIndex.Item2)
				{
					index = i - NetItem.ArmorIndex.Item1;
					playerInventory[i] = (NetItem)armor[index];
				}
				else if (i < NetItem.DyeIndex.Item2)
				{
					index = i - NetItem.DyeIndex.Item1;
					playerInventory[i] = (NetItem)dye[index];
				}
				else if (i < NetItem.MiscEquipIndex.Item2)
				{
					index = i - NetItem.MiscEquipIndex.Item1;
					playerInventory[i] = (NetItem)miscEquips[index];
				}
				else if (i < NetItem.MiscDyeIndex.Item2)
				{
					index = i - NetItem.MiscDyeIndex.Item1;
					playerInventory[i] = (NetItem)miscDyes[index];
				}
				else if (i < NetItem.PiggyIndex.Item2)
				{
					index = i - NetItem.PiggyIndex.Item1;
					playerInventory[i] = (NetItem)piggy[index];
				}
				else if (i < NetItem.SafeIndex.Item2)
				{
					index = i - NetItem.SafeIndex.Item1;
					playerInventory[i] = (NetItem)safe[index];
				}
				else if (i < NetItem.TrashIndex.Item2)
				{
					playerInventory[i] = (NetItem)trash;
				}
				else if (i < NetItem.ForgeIndex.Item2)
				{
					index = i - NetItem.ForgeIndex.Item1;
					playerInventory[i] = (NetItem)forge[index];
				}
			}

			return playerInventory;
		}

		private static void ApplyInventory(TSPlayer player, IReadOnlyList<NetItem> inventory)
		{
			int index;
			var ssc = Main.ServerSideCharacter;
			if (!ssc)
			{
				Main.ServerSideCharacter = true;
				NetMessage.SendData((int) PacketTypes.WorldInfo, player.Index, -1, NetworkText.Empty);
			}

			for (var i = 0; i < NetItem.MaxInventory; i++)
			{
				if (i < NetItem.InventoryIndex.Item2)
				{
					player.TPlayer.inventory[i].netDefaults(inventory[i].NetId);

					if (player.TPlayer.inventory[i].netID != 0)
					{
						player.TPlayer.inventory[i].prefix = inventory[i].PrefixId;
						player.TPlayer.inventory[i].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int) PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.inventory[i].Name), player.Index, i, player.TPlayer.inventory[i].prefix);
					NetMessage.SendData((int) PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.inventory[i].Name), player.Index, i, player.TPlayer.inventory[i].prefix);
				}
				else if (i < NetItem.ArmorIndex.Item2)
				{
					index = i - NetItem.ArmorIndex.Item1;

					player.TPlayer.armor[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.armor[index].netID != 0)
					{
						player.TPlayer.armor[index].prefix = inventory[i].PrefixId;
						player.TPlayer.armor[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int) PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.armor[index].Name), player.Index, i, player.TPlayer.armor[index].prefix);
					NetMessage.SendData((int) PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.armor[index].Name), player.Index, i, player.TPlayer.armor[index].prefix);
				}
				else if (i < NetItem.DyeIndex.Item2)
				{
					index = i - NetItem.DyeIndex.Item1;

					player.TPlayer.dye[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.dye[index].netID != 0)
					{
						player.TPlayer.dye[index].prefix = inventory[i].PrefixId;
						player.TPlayer.dye[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int) PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(player.TPlayer.dye[index].Name),
						player.Index, i, player.TPlayer.dye[index].prefix);
					NetMessage.SendData((int) PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.dye[index].Name), player.Index, i, player.TPlayer.dye[index].prefix);
				}
				else if (i < NetItem.MiscEquipIndex.Item2)
				{
					index = i - NetItem.MiscEquipIndex.Item1;

					player.TPlayer.miscEquips[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.miscEquips[index].netID != 0)
					{
						player.TPlayer.miscEquips[index].prefix = inventory[i].PrefixId;
						player.TPlayer.miscEquips[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int) PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.miscEquips[index].Name), player.Index, i,
						player.TPlayer.miscEquips[index].prefix);
					NetMessage.SendData((int) PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.miscEquips[index].Name), player.Index, i,
						player.TPlayer.miscEquips[index].prefix);
				}
				else if (i < NetItem.MiscDyeIndex.Item2)
				{
					index = i - NetItem.MiscDyeIndex.Item1;

					player.TPlayer.miscDyes[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.miscDyes[index].netID != 0)
					{
						player.TPlayer.miscDyes[index].prefix = inventory[i].PrefixId;
						player.TPlayer.miscDyes[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int) PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.miscDyes[index].Name), player.Index, i,
						player.TPlayer.miscDyes[index].prefix);
					NetMessage.SendData((int) PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.miscDyes[index].Name), player.Index, i,
						player.TPlayer.miscDyes[index].prefix);
				}
				else if (i < NetItem.PiggyIndex.Item2)
				{
					index = i - NetItem.PiggyIndex.Item1;

					player.TPlayer.bank.item[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.bank.item[index].netID != 0)
					{
						player.TPlayer.bank.item[index].prefix = inventory[i].PrefixId;
						player.TPlayer.bank.item[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int) PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.bank.item[index].Name), player.Index, i,
						player.TPlayer.bank.item[index].prefix);
					NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.bank.item[index].Name), player.Index, i,
						player.TPlayer.bank.item[index].prefix);
				}
				else if (i < NetItem.SafeIndex.Item2)
				{
					index = i - NetItem.SafeIndex.Item1;

					player.TPlayer.bank2.item[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.bank2.item[index].netID != 0)
					{
						player.TPlayer.bank2.item[index].prefix = inventory[i].PrefixId;
						player.TPlayer.bank2.item[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.bank2.item[index].Name), player.Index, i,
						player.TPlayer.bank2.item[index].prefix);
					NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.bank2.item[index].Name), player.Index, i,
						player.TPlayer.bank2.item[index].prefix);
				}
				else if (i < NetItem.TrashIndex.Item2)
				{
					player.TPlayer.trashItem.netDefaults(inventory[i].NetId);

					if (player.TPlayer.trashItem.netID != 0)
					{
						player.TPlayer.trashItem.prefix = inventory[i].PrefixId;
						player.TPlayer.trashItem.stack = inventory[i].Stack;
					}

					NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.trashItem.Name), player.Index, i,
						player.TPlayer.trashItem.prefix);
					NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.trashItem.Name), player.Index, i,
						player.TPlayer.trashItem.prefix);
				}
				else if (i < NetItem.ForgeIndex.Item2)
				{
					index = i - NetItem.ForgeIndex.Item1;

					player.TPlayer.bank3.item[index].netDefaults(inventory[i].NetId);

					if (player.TPlayer.bank3.item[index].netID != 0)
					{
						player.TPlayer.bank3.item[index].prefix = inventory[i].PrefixId;
						player.TPlayer.bank3.item[index].stack = inventory[i].Stack;
					}

					NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1,
						NetworkText.FromLiteral(player.TPlayer.bank3.item[index].Name), player.Index, i,
						player.TPlayer.bank3.item[index].prefix);
					NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1,
						NetworkText.FromLiteral(player.TPlayer.bank3.item[index].Name), player.Index, i,
						player.TPlayer.bank3.item[index].prefix);
				}
			}

			if (!ssc)
			{
				Main.ServerSideCharacter = false;
				NetMessage.SendData((int) PacketTypes.WorldInfo, player.Index, -1, NetworkText.Empty);
			}
		}

		/// <summary>
		/// Gets player information for the specified user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The <see cref="PlayerInfo"/> object.</returns>
		public PlayerInfo Get(User user)
		{
			if (!_cache.TryGetValue(user.ID, out var player))
			{
				return default(PlayerInfo);
			}

			return player;
		}

		/// <summary>
		/// Saves the given player's inventory.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="inventoryName">The inventory name.</param>
		public bool SaveInventory(TSPlayer player, string inventoryName)
		{
			var inventory = CreateInventory(player);
			if (!_cache.ContainsKey(player.User.ID))
			{
				_cache.Add(player.User.ID,
					new PlayerInfo(player.User.ID, new Dictionary<string, NetItem[]> {[inventoryName] = inventory}));
			}
			else
			{
				var config = UnlimitedInventoriesConfig.Instance;
				var playerInfo = _cache[player.User.ID];
				if (playerInfo.HasInventory(inventoryName))
				{
					_cache[player.User.ID].Inventories[inventoryName] = inventory;
                    TSPlayer.Server.SendInfoMessage($"Updating existing inventory {inventoryName}");
					_db.Query("UPDATE UnlimitedInventories SET Inventory = @2 WHERE UserID = @0 and Name = @1", player.User.ID, inventoryName, string.Join("~", inventory));
					return true;
				}

				if (playerInfo.Inventories.Count + 1 > config.InventoryLimit && !player.HasPermission(config.BypassPermission))
				{
					return false;
				}

				_cache[player.User.ID].Inventories.Add(inventoryName, inventory);
			}

            TSPlayer.Server.SendInfoMessage($"Creating new inventory {inventoryName}");
            _db.Query("INSERT INTO UnlimitedInventories(UserID, Name, Inventory) VALUES (@0, @1, @2);", player.User.ID,
				inventoryName,
				string.Join("~", inventory));
			return true;
		}

		/// <summary>
		/// Loads the given player's inventory.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="inventoryName">The inventory's name.</param>
		public void LoadInventory(TSPlayer player, string inventoryName)
		{
			if (!_cache.ContainsKey(player.User.ID))
			{
				return;
			}

			var inventory = _cache[player.User.ID].Inventories[inventoryName];
			ApplyInventory(player, inventory);
		}

		/// <summary>
		/// Deletes the given player's inventory.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="inventoryName">The inventory.</param>
		public void DeleteInventory(TSPlayer player, string inventoryName)
		{
			if (!_cache.ContainsKey(player.User.ID))
			{
				return;
			}

			_cache[player.User.ID].Inventories.Remove(inventoryName);
            TSPlayer.Server.SendInfoMessage($"Deleting inventory {inventoryName}");
            _db.Query($"DELETE FROM UnlimitedInventories WHERE UserID = @0 AND Name = @1", player.User.ID, inventoryName);
		}
	}
}
