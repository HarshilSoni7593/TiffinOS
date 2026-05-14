namespace TiffinOS.API.Models.Inventory
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;        // 'kg', 'litre', 'pcs', 'dozen', 'gram'
        public string Category { get; set; } = string.Empty;    // 'vegetables', 'dairy', 'spices'
        public decimal ReorderThreshold { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ItemSupplier> ItemSuppliers { get; set; } = [];
        public ICollection<StockLog> StockLogs { get; set; } = [];
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = [];
    }
}
