namespace BlackBeam.Services.Orders.Model;

public class OrderItem
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid IdOrder {get; set;}
    public string ProductName {get; set;} = string.Empty;
    public string ProductBarcode {get; set;} = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal {get; set;}
}