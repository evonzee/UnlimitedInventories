using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace UnlimitedInventories
{
	/// <summary>
	/// Holds player information.
	/// </summary>
	public sealed class PlayerInfo
	{
		/// <summary>
		/// Gets the user's ID.
		/// </summary>
		public int UserId { get; }

		/// <summary>
		/// Gets the user's inventories.
		/// </summary>
		public Dictionary<string, NetItem[]> Inventories { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PlayerInfo"/> class.
		/// </summary>
		/// <param name="userId">The user's ID.</param>
		/// <param name="inventories">The user's inventories.</param>
		public PlayerInfo(int userId, Dictionary<string, NetItem[]> inventories)
		{
			UserId = userId;
			Inventories = inventories;
		}

		/// <summary>
		/// Determines whether the inventory cache contains an inventory with the given name.
		/// </summary>
		/// <param name="name">The inventory name.</param>
		/// <returns><c>true</c> if the inventory exists; otherwise, <c>false</c>.</returns>
		public bool HasInventory(string name) => Inventories.Keys.Contains(name);

		/// <summary>
		/// Gets the names of stored inventories.
		/// </summary>
		public IEnumerable<string> GetInventoryNames => Inventories.Keys;
	}
}
