using Menu.Shared.Dtos.Baskets;

namespace Menu.Services.Abstraction.Payments
{
    public interface IPaymentService
    {
        Task<BasketDto?> CreateOrUpdatePaymentIntentAsync(
            string basketId);

        Task<bool> UpdateOrderPaymentStatusAsync(
            string paymentIntentId,
            bool isPaymentSuccessful);
    }
}