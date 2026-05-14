using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Inventory
{
    public class PurchaseOrder
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SupplierId { get; set; }
        public string Status { get; set; } = "draft";  // 'draft', 'sent', 'received', 'cancelled'
        public decimal TotalCost { get; set; } = 0;
        public string? Notes { get; set; }
        public DateTime? OrderedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Supplier Supplier { get; set; } = null!;
        public ICollection<PurchaseOrderItem> Items { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
