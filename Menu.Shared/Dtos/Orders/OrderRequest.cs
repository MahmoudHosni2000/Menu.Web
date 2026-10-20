namespace Menu.Shared.Dtos.Orders;

public class OrderRequest
{
    public string BasketId { get; set; } = string.Empty;
    public int DeliveryMethodId { get; set; }
    public OrderAddressDto ShipToAddress { get; set; } = null!;
}