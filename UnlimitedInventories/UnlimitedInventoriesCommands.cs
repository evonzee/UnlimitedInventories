using System;
using TShockAPI;

namespace UnlimitedInventories
{
	internal sealed class UnlimitedInventoriesCommands : IDisposable
	{
		private readonly Database _database;

		/// <summary>
		/// Initializes a new instance of the <see cref="UnlimitedInventoriesCommands"/> class with the specified database instance.
		/// </summary>
		/// <param name="database">The database instance.</param>
		public UnlimitedInventoriesCommands(Database database)
		{
			_database = database;
			Commands.ChatCommands.Add(new Command("ui.root", ManageInventories, "inventory"));
		}

		/// <summary>
		/// Disposes the command handler.
		/// </summary>
		public void Dispose()
		{
			Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == ManageInventories);
		}

		private void ManageInventories(CommandArgs e)
		{
			if (!e.Player.IsLoggedIn)
			{
				e.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (e.Parameters.Count < 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax:");
				e.Player.SendErrorMessage("{0}inventory save <name> - saves/updates your current inventory", TShock.Config.CommandSpecifier);
				e.Player.SendErrorMessage("{0}inventory load <name> - loads an inventory", TShock.Config.CommandSpecifier);
				e.Player.SendErrorMessage("{0}inventory delete <name> - deletes an inventory", TShock.Config.CommandSpecifier);
				e.Player.SendErrorMessage("{0}inventory list - lists all your inventories", TShock.Config.CommandSpecifier);
				return;
			}

			switch (e.Parameters[0].ToLowerInvariant())
			{
				case "save":
					{
						if (e.Parameters.Count < 2)
						{
							e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}inventory save <inventory name>");
							return;
						}

						e.Parameters.RemoveRange(0, 1);
						var inventoryName = string.Join(" ", e.Parameters);
						if (!_database.SaveInventory(e.Player, inventoryName))
						{
							e.Player.SendErrorMessage("You have reached the max amount of inventories.");
							return;
						}

						e.Player.SendSuccessMessage($"Inventory '{inventoryName}' has been saved.");
					}
					break;
				case "load":
					{
						if (e.Parameters.Count < 2)
						{
							e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}inventory load <inventory name>");
							return;
						}

						var player = _database.Get(e.Player.User);
						if (player == null)
						{
							e.Player.SendErrorMessage("You do not have any inventories saved.");
							return;
						}

						e.Parameters.RemoveRange(0, 1);
						var inventoryName = string.Join(" ", e.Parameters);
						if (!player.HasInventory(inventoryName))
						{
							e.Player.SendErrorMessage($"No inventories under the name '{inventoryName}' were found.");
							return;
						}

						_database.LoadInventory(e.Player, inventoryName);
						e.Player.SendSuccessMessage($"Loaded inventory '{inventoryName}'.");
					}
					break;
				case "del":
				case "rem":
				case "delete":
				case "remove":
					{
						if (e.Parameters.Count < 2)
						{
							e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}inventory delete <inventory name>");
							return;
						}

						var player = _database.Get(e.Player.User);
						if (player == null)
						{
							e.Player.SendErrorMessage("You do not have any inventories saved.");
							return;
						}

						e.Parameters.RemoveRange(0, 1);
						var inventoryName = string.Join(" ", e.Parameters);
						if (!player.HasInventory(inventoryName))
						{
							e.Player.SendErrorMessage($"No inventories under the name '{inventoryName}' were found.");
							return;
						}

						_database.DeleteInventory(e.Player, inventoryName);
						e.Player.SendSuccessMessage($"Deleted inventory '{inventoryName}'.");
					}
					break;
				case "list":
					{
						var player = _database.Get(e.Player.User);
						if (player == null)
						{
							e.Player.SendErrorMessage("You do not have any inventories saved.");
							return;
						}

						if (e.Parameters.Count > 2)
						{
							e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}inventory list [page]");
							return;
						}


						if (!PaginationTools.TryParsePageNumber(e.Parameters, 1, e.Player, out var pageNum))
						{
							return;
						}

						PaginationTools.SendPage(e.Player, pageNum, PaginationTools.BuildLinesFromTerms(player.GetInventoryNames),
							new PaginationTools.Settings
							{
								HeaderFormat = "Inventories ({0}/{1})",
								FooterFormat = $"Type {Commands.Specifier}inventory list {{0}} for more.",
								NothingToDisplayString = "You have no inventories to display."
							});
					}
					break;
			}
		}
	}
}
