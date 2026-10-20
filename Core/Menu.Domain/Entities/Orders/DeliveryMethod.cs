using Menu.Domain.Entities;

namespace Menu.Domain.Entities.Orders
{
    public class DeliveryMethod : BaseEntity<int>
    {
        public string ShortName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string DeliveryTime { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}