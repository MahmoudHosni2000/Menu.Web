using AutoMapper;
using Menu.Domain.Contracts;
using Menu.Domain.Entities.Products;
using Menu.Domain.Exceptions;
using Menu.Services.Abstraction.Payments;
using Menu.Shared.Dtos.Baskets;
using Menu.Shared.Settings;
using Microsoft.Extensions.Options;
using StripeClient = Stripe.StripeClient;
using Menu.Domain.Entities.Orders;
using Menu.Domain.Enums;
using Menu.Services.Specifications.Orders;

namespace Menu.Persistence.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly StripeClient _stripeClient;

        public PaymentService(
            IBasketRepository basketRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOptions<StripeOptions> stripeOptions)
        {
            _basketRepository = basketRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _stripeClient = new StripeClient(stripeOptions.Value.SecretKey);
        }

        // This method creates or updates a payment intent for a given basket ID.
        // It retrieves the basket from the repository, and if the basket exists,
        // it maps it to a BasketDto and returns it. If the basket does not exist, it returns null.
        public async Task<BasketDto?> CreateOrUpdatePaymentIntentAsync(
            string basketId)
        {
            var basket = await _basketRepository
                .GetBasketAsync(basketId);

            if (basket is null)
            {
                return null;
            }

            var productRepository =
                _unitOfWork.GetRepo<int, Product>();

            foreach (var item in basket.Items)
            {
                var product = await productRepository
                    .GetByIdAsync(item.ProductId);

                if (product is null)
                {
                    throw new ProductNotFoundException(
                        item.ProductId);
                }

                item.Price = product.Price;
            }

            if (!basket.DeliveryMethodId.HasValue)
            {
                return null;
            }

            // Get the delivery method from the repository using the delivery method ID from the basket.
            var deliveryMethodRepository =
                _unitOfWork.GetRepo<int, DeliveryMethod>();

            // If the delivery method is not found, return null.
            var deliveryMethod = await deliveryMethodRepository
                .GetByIdAsync(
                    basket.DeliveryMethodId.Value);

            if (deliveryMethod is null)
            {
                return null;
            }

            basket.ShippingPrice = deliveryMethod.Price;

            var total =
                basket.Items.Sum(
                    item => item.Price * item.Quantity)
                + basket.ShippingPrice;

            var amount = (long)Math.Round(
                total * 100,
                MidpointRounding.AwayFromZero);

            var paymentIntentService =
                new Stripe.PaymentIntentService(
                    _stripeClient);

            if (string.IsNullOrWhiteSpace(
                basket.PaymentIntentId))
            {
                var createOptions =
                    new Stripe.PaymentIntentCreateOptions
                    {
                        Amount = amount,
                        Currency = "usd",
                        AllowedPaymentMethodTypes = ["card"]
                    };

                var paymentIntent =
                    await paymentIntentService.CreateAsync(
                        createOptions);

                basket.PaymentIntentId = paymentIntent.Id;
                basket.ClientSecret =
                    paymentIntent.ClientSecret;
            }
            else
            {
                var updateOptions =
                    new Stripe.PaymentIntentUpdateOptions
                    {
                        Amount = amount
                    };

                await paymentIntentService.UpdateAsync(
                    basket.PaymentIntentId,
                    updateOptions);
            }

            var updatedBasket = await _basketRepository
                .UpdateBasketAsync(basket);

            if (updatedBasket is null)
            {
                return null;
            }

            return _mapper.Map<BasketDto>(
                updatedBasket);
        }

        public async Task<bool> UpdateOrderPaymentStatusAsync(
            string paymentIntentId,
            bool isPaymentSuccessful)
        {   
            var specification =
                new OrderByPaymentIntentIdSpecification(
                    paymentIntentId); // output ex: "pi_3N1Y5eL8J9K8J8J8J8J8J8J8" 

            var orderRepository =
                    _unitOfWork.GetRepo<int, Order>(); // output from orderRepository for example is 

            var order = await orderRepository
                    .GetByIdAsync(specification);

            if (order is null)
                return false;

            order.Status = isPaymentSuccessful
                ? OrderStatus.PaymentReceived
                : OrderStatus.PaymentFailed;

            orderRepository.Update(order);

            return await _unitOfWork.SaveChangesAsync() > 0;
        }
    }
}