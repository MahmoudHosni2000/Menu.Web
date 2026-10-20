using Menu.Services.Abstraction.Products;
using Menu.Services.Abstraction.Baskets;
using Menu.Services.Abstraction.Orders;
using Menu.Services.Abstraction.Payments;

namespace Menu.Services.Abstraction;

public interface IServiceManager
{
    IProductService ProductService { get; }
    IBasketService BasketService { get; }
    IOrderService OrderService { get; }
    IPaymentService PaymentService { get; }
}
