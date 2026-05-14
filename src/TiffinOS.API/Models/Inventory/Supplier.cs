namespace TiffinOS.API.Models.Inventory
{
    public class Supplier
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? WhatsappNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ItemSupplier> ItemSuppliers { get; set; } = [];
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];

    }
}
