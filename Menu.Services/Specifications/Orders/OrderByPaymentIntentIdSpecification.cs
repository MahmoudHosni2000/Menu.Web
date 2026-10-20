using Menu.Domain.Entities.Orders;

namespace Menu.Services.Specifications.Orders;

public class OrderByPaymentIntentIdSpecification
    : BaseSpecifications<int, Order>
{
    public OrderByPaymentIntentIdSpecification(
        string paymentIntentId)
        : base(order =>
            order.PaymentIntentId == paymentIntentId)
    {
    }
}