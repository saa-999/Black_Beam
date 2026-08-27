using BlackBeam.Services.Inventory.Data;
using BlackBeam.Services.Inventory.Models;
using BlackBeam.Services.Inventory.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BlackBeam.Services.Inventory.Services;

public class InventoryServices : IInventoryServices
{
    private readonly InventoryDbContext _context;

    public InventoryServices(InventoryDbContext context)
    {
        _context = context;
    }
    public async Task<InventoryResult<ProductDisplayDto>> AddProductAsync(AddInventory request)
    {
        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var exists = await _context.Products.AnyAsync(p => p.Barcode == request.Barcode);
            if (exists)
            {
                return new InventoryResult<ProductDisplayDto>(false, default!, "المنتج هذا الباركود موجود مسبقاً!");
            }
        }

       var product = new Product
        {
            Name = request.name,
            Barcode = request.Barcode ?? string.Empty,
            Price = request.price,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();


       var dto = new ProductDisplayDto 
        { 
            Name = product.Name, 
            Barcode = product.Barcode, 
            Price = product.Price, 
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl
        };
        
        return new InventoryResult<ProductDisplayDto>(true , dto , null);
    }
    

    public async Task<InventoryResult<IEnumerable<ProductDisplayDto>>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDisplayDto
            {
                Name = p.Name,
                Barcode = p.Barcode,
                Price = p.Price,
                StockQuantity = p.StockQuantity
            })
            .ToListAsync();

        return new InventoryResult<IEnumerable<ProductDisplayDto>>(true, products, null);
    }

    public async Task<InventoryResult<ProductDisplayDto>> UpdateProductAsync(UpdateInventory request)
    {
        if (string.IsNullOrEmpty(request.Barcode))
        {
            return new InventoryResult<ProductDisplayDto>(false, default!, 
            "الباركود فارغ!!");
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == request.Barcode);

        if(product == null)
        {
             return new InventoryResult<ProductDisplayDto>(false , default! ,
              "الباركود غير موجود!!");
        }

        product.Name = request.name;
        product.Price = request.price;
        product.StockQuantity = request.StockQuantity;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        var dto = new ProductDisplayDto {
        Name = product.Name,
        ImageUrl = product.ImageUrl,
        Price = product.Price,
        Barcode = product.Barcode,
        StockQuantity = product.StockQuantity
    };

        return new  InventoryResult<ProductDisplayDto>(true , dto , "تم علمية التعديل بنجاح");
    }

}