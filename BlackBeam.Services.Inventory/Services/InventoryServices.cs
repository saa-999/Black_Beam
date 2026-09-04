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
        string cleanedBarcode = request.Barcode?.Trim() ?? string.Empty;
        string cleanedName = request.name?.Trim() ?? string.Empty;

        var exists = await _context.Products.AnyAsync(p => p.Barcode == cleanedBarcode);
        if (exists)
        {
            return new InventoryResult<ProductDisplayDto>(false, default!, "المنتج هذا الباركود موجود مسبقاً!");
        }

        if (request.price <= 0 || request.StockQuantity < 0)
        {
            return new InventoryResult<ProductDisplayDto>(false, default!, "السعر او الكمية غير صحيحة");
        }

        var product = new Product
        {
            Name = cleanedName,
            Barcode = cleanedBarcode,
            Price = request.price,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();


        var dto = new ProductDisplayDto
        {
            Name = product.Name,
            Barcode = product.Barcode,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl
        };

        return new InventoryResult<ProductDisplayDto>(true, dto, null);
    }


    public async Task<InventoryResult<IEnumerable<ProductDisplayDto>>> GetAllProductsAsync(PaginationOptions pagination)
    {
        int pageNumber = pagination.pageNumber < 1 ? 1 : pagination.pageNumber;
        int pageSize = pagination.pageSize > 100 ? 100 : (pagination.pageSize < 1 ? 20 : pagination.pageSize);
        int skip = (pageNumber - 1) * pageSize;

        var products = await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDisplayDto
            {
                Name = p.Name,
                Barcode = p.Barcode,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return new InventoryResult<IEnumerable<ProductDisplayDto>>(true, products, null);
    }

    public async Task<InventoryResult<ProductDisplayDto>> UpdateProductAsync(UpdateInventory request)
    {
        string cleanedBarcode = request.Barcode?.Trim() ?? string.Empty;
        string cleanedName = request.name?.Trim() ?? string.Empty;
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == cleanedBarcode);

        if (product == null)
        {
            return new InventoryResult<ProductDisplayDto>(false, default!,
             "الباركود غير موجود!!");
        }

        product.Name = cleanedName;
        product.ImageUrl = request.ImageUrl ?? product.ImageUrl;

        product.Price = (request.price.HasValue && request.price.Value > 0)
        ? request.price.Value
        : product.Price;

        product.StockQuantity = (request.StockQuantity.HasValue && request.StockQuantity.Value >= 0)
        ? request.StockQuantity.Value
        : product.StockQuantity;

        product.IsActive = request.IsActive ?? product.IsActive;

        await _context.SaveChangesAsync();

        var dto = new ProductDisplayDto
        {
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            Barcode = product.Barcode,
            StockQuantity = product.StockQuantity
        };

        return new InventoryResult<ProductDisplayDto>(true, dto, "تم علمية التعديل بنجاح");
    }

    public async Task<InventoryResult<bool>> DeleteProductAsync(string? barcode)
    {
        var cleanedBarcode = barcode?.Trim() ?? string.Empty;

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == cleanedBarcode);
        if (product == null)
        {
            return new InventoryResult<bool>(false, false, "الباركود غير موجود");
        }

        product.IsActive = false;
        await _context.SaveChangesAsync();
        return new InventoryResult<bool>(true, true, null);
    }


    public async Task<InventoryResult<ProductDisplayDto>> GetProductByBarcodeAsync(string barcode)
    {
        var cleanedBarcode = barcode?.Trim() ?? string.Empty;

        var dto = await _context.Products
        .AsNoTracking()
        .Where(p => p.Barcode == cleanedBarcode)
        .Select(p => new ProductDisplayDto
        {
            Name = p.Name,
            Barcode = p.Barcode,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            ImageUrl = p.ImageUrl
        }).FirstOrDefaultAsync();

        if (dto == null)
        {
            return new InventoryResult<ProductDisplayDto>(false, default!, "الباركود غير موجود");
        }
        return new InventoryResult<ProductDisplayDto>(true, dto, null);
    }

    public async Task<InventoryResult<IEnumerable<ProductDisplayDto>>> SearchProductsAsync(string searchTerm)
    {
        string cleanedSearchTerm = searchTerm?.Trim() ?? string.Empty;

        var products = await _context.Products
       .Where(p => EF.Functions.Match(p.Name, cleanedSearchTerm, MySqlMatchSearchMode.NaturalLanguage) > 0)
       .Select(p => new ProductDisplayDto
       {
           Name = p.Name,
           Barcode = p.Barcode,
           Price = p.Price,
           StockQuantity = p.StockQuantity
       })
       .Take(10)
       .ToListAsync();
        return new InventoryResult<IEnumerable<ProductDisplayDto>>(true, products, null);
    }

    public async Task<InventoryResult<IEnumerable<ProductAdminDetailsDto>>> GetAdministrativeProductDetailsAsync(PaginationOptions paginationOptions)
    {
        int pageNumber = paginationOptions.pageNumber < 1 ? 1 : paginationOptions.pageNumber;
        int pageSize = paginationOptions.pageSize > 100 ? 100 : (paginationOptions.pageSize < 1 ? 20 : paginationOptions.pageSize);
        int skip = (pageNumber - 1) * pageSize;

        var products = await _context.Products
           .AsNoTracking()
           .OrderByDescending(p => p.CreatedAt)
           .Skip(skip)
           .Take(pageSize)
           .Select(p => new ProductAdminDetailsDto
           {
               Name = p.Name,
               Barcode = p.Barcode,
               Price = p.Price,
               StockQuantity = p.StockQuantity,
               IsActive = p.IsActive,
               ImageUrl = p.ImageUrl,
               CreatedAt = p.CreatedAt
           })
           .ToListAsync();

        return new InventoryResult<IEnumerable<ProductAdminDetailsDto>>(true, products, null);
    }

    public async Task<InventoryResult<bool>> DeductStockAsync(List<DeductStockItem> items)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == item.Barcode);
                if (product == null)
                {
                    return new InventoryResult<bool>(false, false, $"المنتج بالباركود {item.Barcode} غير موجود");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return new InventoryResult<bool>(false, false, $"الكمية المتاحة للمنتج بالباركود {item.Barcode} غير كافية");
                }

                product.StockQuantity -= item.Quantity;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new InventoryResult<bool>(true, true, "تم خصم الكميات بنجاح");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new InventoryResult<bool>(false, false, $"حدث خطأ أثناء خصم الكميات: {ex.Message}");
        }
    }
}