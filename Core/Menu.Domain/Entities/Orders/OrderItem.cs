using Menu.Domain.Entities;

namespace Menu.Domain.Entities.Orders
{
    public class OrderItem : BaseEntity<int>
    {
        public ProductItemOrdered Product { get; set; } = null!;

        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}