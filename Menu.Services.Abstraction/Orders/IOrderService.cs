using Menu.Shared.Dtos.Orders;

namespace Menu.Services.Abstraction.Orders
{
    public interface IOrderService
    {
        Task<OrderResponse?> CreateOrderAsync(
            OrderRequest request,
            string userEmail);

        Task<IEnumerable<DeliveryMethodResponse>>
            GetAllDeliveryMethodsAsync();

        Task<OrderResponse?> GetOrderByIdForSpecificUserAsync(
            int id,
            string userEmail);

        Task<IEnumerable<OrderResponse>>
            GetOrdersForSpecificUserAsync(string userEmail);
    }
}