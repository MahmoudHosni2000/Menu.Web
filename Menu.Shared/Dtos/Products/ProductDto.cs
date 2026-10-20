using System;
using System.Collections.Generic;
using System.Text;

namespace Menu.Shared.Dtos.Products
{
    // This class represents a Data Transfer Object (DTO) for a product, containing properties that describe the product's details.
    // It is used to transfer product data between different layers of the application, such as from the service layer to the presentation layer.
    public class ProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string Brand { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
    }
}
