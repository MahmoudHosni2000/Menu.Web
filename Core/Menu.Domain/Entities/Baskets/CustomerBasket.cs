namespace Menu.Domain.Entities.Baskets
{
    public class CustomerBasket
    {
        public string Id { get; set; } = string.Empty;

        public List<BasketItem> Items { get; set; } = new();

        public int? DeliveryMethodId { get; set; }

        public decimal ShippingPrice { get; set; }

        public string? PaymentIntentId { get; set; }

        public string? ClientSecret { get; set; }
    }
}