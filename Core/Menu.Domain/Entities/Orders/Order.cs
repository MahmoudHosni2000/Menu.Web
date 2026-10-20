using Menu.Domain.Enums;

namespace Menu.Domain.Entities.Orders
{
    public class Order : BaseEntity<int>
    {
        public string BuyerEmail { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string PaymentIntentId { get; set; } = string.Empty;
        public OrderAddress ShippingAddress { get; set; } = null!;
        public int DeliveryMethodId { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();
        public decimal SubTotal { get; set; }
        public decimal GetTotal()
        {
            return SubTotal + DeliveryMethod.Price;
        }
    }
}