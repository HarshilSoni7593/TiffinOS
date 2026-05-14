namespace TiffinOS.API.Models.Inventory
{
    public class ItemSupplier
    {
        public Guid ItemId { get; set; }
        public Guid SupplierId { get; set; }
        public bool IsPreferred { get; set; } = false;

        // Navigation
        public InventoryItem Item { get; set; } = null!;
        public Supplier Supplier { get; set; } = null!;

    }
}
