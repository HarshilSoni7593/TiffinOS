namespace TiffinOS.API.Models.Tiffin
{
    public class TiffinPlanItem
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public Guid MenuItemId { get; set; }
        public string PortionSize { get; set; } = string.Empty; // must match a value in MenuItem.AvailablePortions
        public decimal Quantity { get; set; } = 1;
        public int DisplayOrder { get; set; } = 0;

        // Navigation
        public TiffinPlan Plan { get; set; } = null!;
        public MenuItem MenuItem { get; set; } = null!;

    }
}
