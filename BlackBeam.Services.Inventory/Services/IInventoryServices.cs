using BlackBeam.Services.Inventory.Models;
using BlackBeam.Services.Inventory.DTOs;
namespace BlackBeam.Services.Inventory.Services;

public interface IInventoryServices
{
    Task<InventoryResult<ProductDisplayDto>> AddProductAsync(AddInventory request);
    

    Task<InventoryResult<IEnumerable<ProductDisplayDto>>> GetAllProductsAsync();
    
    Task<InventoryResult<ProductDisplayDto>> UpdateProductAsync(UpdateInventory request);

}