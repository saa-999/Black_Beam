namespace BlackBeam.Services.Inventory.DTOs;

public class ProductAdminDetailsDto
{
    public string Name  {get; set;} = string.Empty;
    public string Barcode  {get; set;} = string.Empty;
    public decimal Price   {get; set;} 
    public int StockQuantity { get; set; }
    public bool IsActive    { get; set; } = true;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}