namespace TiffinOS.API.Models.Tiffin
{
    public class DailyPackingSummary
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateOnly SummaryDate { get; set; }
        public Guid MenuItemId { get; set; }
        public string PortionSize { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public int TotalBoxes { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public MenuItem MenuItem { get; set; } = null!;
    }
}
