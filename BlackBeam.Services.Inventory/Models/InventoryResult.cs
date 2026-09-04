namespace BlackBeam.Services.Inventory.Models;

public record InventoryResult<T>(bool IsSuccess , T data , string? ErrorMessage);
public record AddInventory(string name , string? Barcode
 , decimal price  , int StockQuantity , string? ImageUrl);

public record UpdateInventory(string? name , string? Barcode
 , decimal? price  , int? StockQuantity , string? ImageUrl , bool? IsActive = true);

public record PaginationOptions(int pageNumber = 1, int pageSize = 20);

public record DeductStockItem(string Barcode, int Quantity);