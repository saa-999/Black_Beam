using BlackBeam.Services.Inventory.Models;
using BlackBeam.Services.Inventory.DTOs;
using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Inventory.Services;
using BlackBeam.Services.Inventory.Data;
using Superpower.Model;
using BlackBeam.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace BlackBeam.Services.Inventory.Endpoints;

public static class EndPointsInventory
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/Inventory");
        
        group.MapPost("/AddProducts", async (AddInventory request ,  IInventoryServices services ) =>
        {
           if(string.IsNullOrEmpty(request.name) || string.IsNullOrEmpty(request.Barcode) )
            {
                return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {"الاسم او الباركود فارغ"}));
            }
            
            var result = await services.AddProductAsync(request);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage!}));
            }

            return  Results.Ok(ApiResponse<ProductDisplayDto>.Success(result.data));
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Cashier" });

       group.MapGet("/View", async (int pageNumber, int pageSize, IInventoryServices services) =>
       {
           var pagination = new PaginationOptions(pageNumber, pageSize);
           var result = await services.GetAllProductsAsync(pagination);
           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "حدث خطأ أثناء جلب المنتجات" }));
           }
           return Results.Ok(ApiResponse<IEnumerable<ProductDisplayDto>>.Success(result.data));
       });

       group.MapPut("/UpdateProduct", async (UpdateInventory request , IInventoryServices services) =>
       {
           if(string.IsNullOrEmpty(request.name) || string.IsNullOrEmpty(request.Barcode) )
            {
                return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {"الاسم او الباركود فارغ"}));
            }
           
           var result = await services.UpdateProductAsync(request);

           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage!}));
           }
           return Results.Ok(ApiResponse<ProductDisplayDto>.Success(result.data));
       }).RequireAuthorization(new AuthorizeAttribute {Roles = "Admin,Cashier"});

       group.MapDelete("/DeleteProduct", async (string? barcode , IInventoryServices services) =>
       {
           if(string.IsNullOrEmpty(barcode)){
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {"الباركود فارغ"}));
           }
           
           var result = await services.DeleteProductAsync(barcode);

           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage!}));
           }
           return Results.Ok(ApiResponse<bool>.Success(result.data));
       }).RequireAuthorization(new AuthorizeAttribute {Roles = "Admin,Cashier"});

       group.MapGet("/GetProductByBarcode/{barcode}", async (string barcode , IInventoryServices services) =>
       {
           if(string.IsNullOrEmpty(barcode)){
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {"الباركود فارغ"}));
           }
           
           var result = await services.GetProductByBarcodeAsync(barcode);

           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage!}));
           }
           return Results.Ok(ApiResponse<ProductDisplayDto>.Success(result.data));
       });

         group.MapGet("/SearchProducts", async (string searchTerm , IInventoryServices services) =>
         {
              if(string.IsNullOrEmpty(searchTerm)){
                return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {"كلمة البحث فارغة"}));
              }
              
              var result = await services.SearchProductsAsync(searchTerm);
    
              if (!result.IsSuccess)
              {
                return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage!}));
              }
              return Results.Ok(ApiResponse<IEnumerable<ProductDisplayDto>>.Success(result.data));
         });

         group.MapGet("/GetAdministrativeProductDetails", async (int pageNumber, int pageSize, IInventoryServices services) =>
         {
             var pagination = new PaginationOptions(pageNumber, pageSize);
             var result = await services.GetAdministrativeProductDetailsAsync(pagination);
             if (!result.IsSuccess)
             {
                 return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "حدث خطأ أثناء جلب تفاصيل المنتجات" }));
             }
             return Results.Ok(ApiResponse<IEnumerable<ProductAdminDetailsDto>>.Success(result.data));
         }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

         group.MapPut("/DeductStock", async (List<DeductStockItem> items , IInventoryServices services) =>
         {
             if(items == null || !items.Any())
             {
                return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {"قائمة العناصر فارغة"}));
             }
             
             var result = await services.DeductStockAsync(items);

             if (!result.IsSuccess)
             {
                 return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage!}));
             }
             return Results.Ok(ApiResponse<bool>.Success(result.data));
         }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Cashier" });
    }
}

