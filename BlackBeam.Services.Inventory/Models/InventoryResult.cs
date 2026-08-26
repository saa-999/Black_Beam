namespace BlackBeam.Services.Inventory.Models;

public record InventoryResult<T>(bool IsSuccess , T data , string? ErrorMessage);
public record AddInventory(string name , string? Barcode
 , decimal price  , int StockQuantity , string? ImageUrl);
