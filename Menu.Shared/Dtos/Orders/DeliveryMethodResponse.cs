namespace Menu.Shared.Dtos.Orders;

public class DeliveryMethodResponse
{
    public int Id { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DeliveryTime { get; set; } = string.Empty;
    public decimal Price { get; set; }
}