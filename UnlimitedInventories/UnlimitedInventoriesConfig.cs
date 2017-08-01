namespace UnlimitedInventories
{
	/// <summary>
	/// Represents the UnlimitedInventories configuration.
	/// </summary>
	public sealed class UnlimitedInventoriesConfig
	{
		/// <summary>
		/// Gets the UnlimitedInventories configuration instance.
		/// </summary>
		public static UnlimitedInventoriesConfig Instance { get; internal set; } = new UnlimitedInventoriesConfig();

		/// <summary>
		/// Gets the inventory limit.
		/// </summary>
		public int InventoryLimit { get; } = 5;

		/// <summary>
		/// Gets the permission used to bypass the max inventory limit.
		/// </summary>
		public string BypassPermission { get; } = "ui.bypass";
	}
}
