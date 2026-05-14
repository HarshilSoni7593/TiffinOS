namespace TiffinOS.API.Models.Tiffin
{
    public class MenuItemCategory
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;   // 'Bread', 'Curry', 'Rice', 'Dessert', 'Drink'
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<MenuItem> MenuItems { get; set; } = [];

    }
}
