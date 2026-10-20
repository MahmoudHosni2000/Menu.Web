using AutoMapper;
using Menu.Domain.Contracts;
using Menu.Services.Abstraction.Orders;
using Menu.Shared.Dtos.Orders;
using Menu.Domain.Entities.Orders;
using Menu.Services.Specifications;
using Menu.Domain.Entities.Products;
using Menu.Services.Specifications.Orders;

namespace Menu.Services.Orders;

public class OrderService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IBasketRepository basketRepository) : IOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IBasketRepository _basketRepository =
        basketRepository;

    // Create an order
    public async Task<OrderResponse?> CreateOrderAsync(
        OrderRequest request,
        string userEmail)
    {
        // 1. Get the basket from the repository
        var basket = await _basketRepository
            .GetBasketAsync(request.BasketId);

        if (basket is null)
            return null;

        if (string.IsNullOrWhiteSpace(basket.PaymentIntentId))
            return null;

        // 2. Get the delivery method from the repository
        // at first, we need to create a specification to get the delivery method by id
        var deliverySpecification =
            new BaseSpecifications<int, DeliveryMethod>(
                method => method.Id == request.DeliveryMethodId);

        // then we can use the specification to get the delivery method from the repository
        var deliveryMethod = await _unitOfWork
            .GetRepo<int, DeliveryMethod>()
            .GetByIdAsync(deliverySpecification);

        if (deliveryMethod is null)
            return null;

        // 3. Create an order address from the request
        var shippingAddress =
            _mapper.Map<OrderAddress>(request.ShipToAddress);

        // 4. Create a list of order items from the basket items
        var orderItems = new List<OrderItem>();

        foreach (var basketItem in basket.Items)
        {
            // here we need to create a specification to get the product by id
            var productSpecification =
                new BaseSpecifications<int, Product>(
                    product => product.Id == basketItem.ProductId); // example output is : product => product.Id == 1

            // then we can use the specification to get the product from the repository
            var product = await _unitOfWork
                .GetRepo<int, Product>()
                .GetByIdAsync(productSpecification);

            if (product is null)
                return null;

            // here we need to create a ProductItemOrdered from the product
            var productItemOrdered = new ProductItemOrdered
            {
                ProductId = product.Id,
                ProductName = product.Name,
                PictureUrl = product.ImageUrl
            };

            // then we can create an OrderItem from the ProductItemOrdered and the basket item
            var orderItem = new OrderItem
            {
                Product = productItemOrdered,
                Price = product.Price,
                Quantity = basketItem.Quantity
            };

            orderItems.Add(orderItem);
        }

        // 5. Calculate the subtotal of the order
        var subTotal = orderItems.Sum(
            item => item.Price * item.Quantity);

        // 6. Create the order
        var order = new Order
        {
            BuyerEmail = userEmail,
            PaymentIntentId = basket.PaymentIntentId,
            ShippingAddress = shippingAddress,
            DeliveryMethodId = deliveryMethod.Id,
            Items = orderItems,
            SubTotal = subTotal
        };

        var existingOrderSpecification =
            new OrderByPaymentIntentIdSpecification(
                basket.PaymentIntentId);

        var existingOrder = await _unitOfWork
            .GetRepo<int, Order>()
            .GetByIdAsync(existingOrderSpecification);

        if (existingOrder is not null)
        {
            _unitOfWork
                .GetRepo<int, Order>()
                .Delete(existingOrder);
        }

        // 7. Add the order to the repository
        await _unitOfWork
            .GetRepo<int, Order>()
            .AddAsync(order);

        // 8. Save the changes to the database
        var savedChanges = await _unitOfWork.SaveChangesAsync();

        if (savedChanges <= 0)
            return null;

        // 9. Return the order response
        order.DeliveryMethod = deliveryMethod;

        // 10. Map the order to the order response
        return _mapper.Map<OrderResponse>(order);
    }

    // Get all delivery methods
    public async Task<IEnumerable<DeliveryMethodResponse>>
        GetAllDeliveryMethodsAsync()
    {
        // 1. Get all delivery methods from the repository
        var deliveryMethods = await _unitOfWork
            .GetRepo<int, DeliveryMethod>()
            .GetAllAsync();

        // 2. Map the delivery methods to the delivery method responses
        return _mapper.Map<IEnumerable<DeliveryMethodResponse>>(
            deliveryMethods);
    }

    // Get order by id for specific user
    public async Task<OrderResponse?>
        GetOrderByIdForSpecificUserAsync(
            int id,
            string userEmail)
    {
        var specification =
            new OrderSpecifications(id, userEmail);

        var order = await _unitOfWork
            .GetRepo<int, Order>()
            .GetByIdAsync(specification);

        return order is null
            ? null
            : _mapper.Map<OrderResponse>(order);
    }

    // Get all orders for specific user
    public async Task<IEnumerable<OrderResponse>>
        GetOrdersForSpecificUserAsync(string userEmail)
    {
        var specification =
            new OrderSpecifications(userEmail);

        var orders = await _unitOfWork
            .GetRepo<int, Order>()
            .GetAllAsync(specification);

        return _mapper.Map<IEnumerable<OrderResponse>>(orders);
    }
}