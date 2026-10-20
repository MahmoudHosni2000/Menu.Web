namespace Menu.Shared.Dtos.Orders;

public class OrderResponse
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public DateTimeOffset OrderDate { get; set; }
    public OrderAddressDto ShippingAddress { get; set; } = null!;
    public string DeliveryMethod { get; set; } = string.Empty;
    public int DeliveryMethodId { get; set; }
    public ICollection<OrderItemDto> Items { get; set; }
        = new List<OrderItemDto>();
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}