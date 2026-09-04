using BlackBeam.Services.Inventory.Models;
using BlackBeam.Services.Inventory.DTOs;
namespace BlackBeam.Services.Inventory.Services;

public interface IInventoryServices
{
    Task<InventoryResult<ProductDisplayDto>> AddProductAsync(AddInventory request);
    

    Task<InventoryResult<IEnumerable<ProductDisplayDto>>> GetAllProductsAsync(PaginationOptions pagination);
    
    Task<InventoryResult<ProductDisplayDto>> UpdateProductAsync(UpdateInventory request);

    Task<InventoryResult<bool>> DeleteProductAsync(string? barcode);

    Task<InventoryResult<ProductDisplayDto>> GetProductByBarcodeAsync(string barcode);

    Task<InventoryResult<IEnumerable<ProductDisplayDto>>> SearchProductsAsync(string searchTerm);

    Task<InventoryResult<IEnumerable<ProductAdminDetailsDto>>> GetAdministrativeProductDetailsAsync(PaginationOptions pagination);

    Task<InventoryResult<bool>> DeductStockAsync(List<DeductStockItem> items);
    Task<InventoryResult<decimal>> GetPrice(string barcode);
}