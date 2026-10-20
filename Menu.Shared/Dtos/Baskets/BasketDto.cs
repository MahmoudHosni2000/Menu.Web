using System.ComponentModel.DataAnnotations;

namespace Menu.Shared.Dtos.Baskets
{
    public class BasketDto
    {
        [Required(ErrorMessage = "Basket id is required.")]
        public string Id { get; set; } = string.Empty;

        public List<BasketItemDto> Items { get; set; } = new();

        public int? DeliveryMethodId { get; set; }

        public decimal ShippingPrice { get; set; }

        public string? PaymentIntentId { get; set; }

        public string? ClientSecret { get; set; }
    }
}