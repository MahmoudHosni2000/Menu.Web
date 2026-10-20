using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Menu.Domain.Entities.Products
{
    public class Product : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("PictureUrl")]
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public int BrandId { get; set; } // Foreign key to ProductBrand
        public ProductBrand Brand { get; set; } = null!;

        public int TypeId { get; set; } // Foreign key to ProductType
        public ProductType Type { get; set; } = null!;
    }
}
