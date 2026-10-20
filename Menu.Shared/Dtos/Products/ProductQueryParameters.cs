using System.ComponentModel.DataAnnotations;

namespace Menu.Shared.Dtos.Products
{
    public class ProductQueryParameters
    {
        public int? BrandId { get; set; }
        public int? TypeId { get; set; }
        public string? Sort { get; set; }
        public string? Search { get; set; }

        [Range(1, int.MaxValue)]
        public int PageIndex { get; set; } = 1;

        [Range(1, 50)]
        public int PageSize { get; set; } = 5;
    }
}
