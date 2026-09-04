using BlackBeam.Shared;
using BlackBeam.Services.Orders.Enum;
namespace BlackBeam.Services.Orders.Model;
public class Order
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public string? CashierId {get; set;}
    public decimal TotalAmount {get; set;}
    public EnumOrderStatus status {get; set;} = EnumOrderStatus.Pending;
    public DateTime OrderDate {get; set;} = DateTime.UtcNow;
    public string PaymentMethod {get; set;} = EnumPayMethod.Cash;
    public bool IsPaid {get; set;} = false;
    public ICollection<OrderItem> OrderItems {get; set;} =  [];
}