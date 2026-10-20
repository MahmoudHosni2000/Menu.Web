using Menu.Services.Abstraction;
using Menu.Services.Abstraction.Products;
using Menu.Services.Abstraction.Baskets;
using Menu.Services.Abstraction.Orders;
using Menu.Services.Abstraction.Payments;

namespace Menu.Services;

public sealed class ServiceManager(
    IProductService productService,
    IBasketService basketService,
    IPaymentService paymentService,
    IOrderService orderService) : IServiceManager
{
    public IProductService ProductService { get; } = productService;
    public IBasketService BasketService { get; } = basketService;
    public IOrderService OrderService { get; } = orderService;
    public IPaymentService PaymentService { get; } = paymentService;
}
