namespace TiffinOS.API.Models.Inventory
{
    public class PurchaseOrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;  // calculated

        // Navigation
        public PurchaseOrder Order { get; set; } = null!;
        public InventoryItem Item { get; set; } = null!;
    }
}
