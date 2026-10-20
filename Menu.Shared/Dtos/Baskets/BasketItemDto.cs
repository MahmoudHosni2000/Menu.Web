using System.ComponentModel.DataAnnotations;

namespace Menu.Shared.Dtos.Baskets
{
    public class BasketItemDto
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Product id must be greater than 0.")]
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string PictureUrl { get; set; } = string.Empty;

        public decimal Price { get; set; }

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }
    }
}