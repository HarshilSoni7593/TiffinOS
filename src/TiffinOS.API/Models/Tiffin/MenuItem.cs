namespace TiffinOS.API.Models.Tiffin
{
    public class MenuItem
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;       // 'Dal Tadka', 'Chapati', 'Jeera Rice'
        public string Unit { get; set; } = string.Empty;       // 'piece', 'portion', 'bowl', 'glass', 'pack'
        public string MeasurementType { get; set; } = string.Empty; // 'volume', 'weight', 'pack', 'count'
        public List<string> AvailablePortions { get; set; } = []; // ["8oz","12oz","16oz"] or ["Pack of 4","Pack of 6"]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public MenuItemCategory? Category { get; set; }
        public ICollection<TiffinPlanItem> TiffinPlanItems { get; set; } = [];
        public ICollection<DailyPackingSummary> DailyPackingSummaries { get; set; } = [];
    }
}
