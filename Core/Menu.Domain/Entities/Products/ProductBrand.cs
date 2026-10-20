using System;
using System.Collections.Generic;
using System.Text;

namespace Menu.Domain.Entities.Products
{
    public class ProductBrand : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
    }
}
