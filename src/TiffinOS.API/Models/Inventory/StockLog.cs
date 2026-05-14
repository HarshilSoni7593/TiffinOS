using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Inventory
{
    public class StockLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ItemId { get; set; }
        public Guid? LoggedBy { get; set; }             // FK → users.id
        public DateOnly LogDate { get; set; }
        public decimal OpeningQty { get; set; }         // pre-filled from yesterday's closing
        public decimal ClosingQty { get; set; }
        public decimal PurchasedQty { get; set; } = 0;
        public decimal InferredUsage =>                 // calculated — never stored manually
            OpeningQty + PurchasedQty - ClosingQty;
        public string? Notes { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public InventoryItem Item { get; set; } = null!;

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
