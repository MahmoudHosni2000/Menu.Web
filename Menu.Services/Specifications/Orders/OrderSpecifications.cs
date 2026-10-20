using Menu.Domain.Entities.Orders;

namespace Menu.Services.Specifications.Orders;

public class OrderSpecifications
    : BaseSpecifications<int, Order>
{
    // 1. Create a specification to get all orders for a user
    public OrderSpecifications(string userEmail)
        : base(order => order.BuyerEmail == userEmail)
    {
        AddInclude(order => order.DeliveryMethod);
        AddInclude(order => order.Items);
        AddOrderByDescending(order => order.OrderDate);
    }

    // 2. Create a specification to get a specific order for a user
    public OrderSpecifications(
        int orderId,
        string userEmail)
        : base(order =>
            order.Id == orderId &&
            order.BuyerEmail == userEmail)
    {
        AddInclude(order => order.DeliveryMethod);
        AddInclude(order => order.Items);
    }
}