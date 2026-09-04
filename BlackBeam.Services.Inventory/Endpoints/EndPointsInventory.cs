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
        group.MapPost("", async (AddInventory request ,  IInventoryServices services ) =>
        {
           if(string.IsNullOrWhiteSpace(request.name) || string.IsNullOrWhiteSpace(request.Barcode) )
            {
                return Results.BadRequest(ApiResponse<string>.Failure(["الاسم فارغ او الباركود فارغ "]));
            }
            
            var result = await services.AddProductAsync(request);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطاء"]));
            }

            return  Results.Ok(ApiResponse<ProductDisplayDto>.Success(result.data));
        }).RequireAuthorization("StaffOnly");

       group.MapGet("/View", async (int? pageNumber, int? pageSize, IInventoryServices services) =>
       {
           var pagination = new PaginationOptions(pageNumber ?? 1, pageSize ?? 10);
           var result = await services.GetAllProductsAsync(pagination);
           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطأ أثناء جلب المنتجات" ]));
           }
           return Results.Ok(ApiResponse<IEnumerable<ProductDisplayDto>>.Success(result.data));
       });

       group.MapPut("/UpdateProduct", async (UpdateInventory request , IInventoryServices services) =>
       {
           if(string.IsNullOrEmpty(request.name) || string.IsNullOrEmpty(request.Barcode) )
            {
                return Results.BadRequest(ApiResponse<string>.Failure(["الاسم او الباركود فارغ"]));
            }
           
           var result = await services.UpdateProductAsync(request);

           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطاء"]));
           }
           return Results.Ok(ApiResponse<ProductDisplayDto>.Success(result.data));
       }).RequireAuthorization("StaffOnly");

       group.MapDelete("/DeleteProduct", async (string? barcode , IInventoryServices services) =>
       {
           if(string.IsNullOrEmpty(barcode)){
               return Results.BadRequest(ApiResponse<string>.Failure(["الباركود فارغ"]));
           }
           
           var result = await services.DeleteProductAsync(barcode);

           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطاء"]));
           }
           return Results.Ok(ApiResponse<bool>.Success(result.data));
       }).RequireAuthorization("StaffOnly");

       group.MapGet("/GetProductByBarcode/{barcode}", async (string barcode , IInventoryServices services) =>
       {
           if(string.IsNullOrWhiteSpace(barcode)){
               return Results.BadRequest(ApiResponse<string>.Failure(["الباركود فارغ"]));
           }
           
           var result = await services.GetProductByBarcodeAsync(barcode);

           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطاء"]));
           }
           return Results.Ok(ApiResponse<ProductDisplayDto>.Success(result.data));
       }).RequireAuthorization("StaffOnly");

         group.MapGet("/SearchProducts", async (string searchTerm , IInventoryServices services) =>
         {
              if(string.IsNullOrWhiteSpace(searchTerm)){
                return Results.BadRequest(ApiResponse<string>.Failure(["كلمة البحث فارغة"]));
              }
              
              var result = await services.SearchProductsAsync(searchTerm);
    
              if (!result.IsSuccess)
              {
                return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطاء"]));
              }
              return Results.Ok(ApiResponse<IEnumerable<ProductDisplayDto>>.Success(result.data));
         }).RequireAuthorization();

         group.MapGet("/GetAdministrativeProductDetails", async (int? pageNumber, int? pageSize, IInventoryServices services) =>
         {
             var pagination = new PaginationOptions(pageNumber ?? 1, pageSize ?? 10);
             var result = await services.GetAdministrativeProductDetailsAsync(pagination);
             if (!result.IsSuccess)
             {
                 return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطأ أثناء جلب تفاصيل المنتجات"]));
             }
             return Results.Ok(ApiResponse<IEnumerable<ProductAdminDetailsDto>>.Success(result.data));
         }).RequireAuthorization("Admin");

         group.MapPut("/DeductStock", async (List<DeductStockItem> items , IInventoryServices services) =>
         {
             if(items == null || !items.Any())
             {
                return Results.BadRequest(ApiResponse<string>.Failure(["قائمة العناصر فارغة"]));
             }
             
             var result = await services.DeductStockAsync(items);

             if (!result.IsSuccess)
             {
                 return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "حدث خطاء"]));
             }
             return Results.Ok(ApiResponse<bool>.Success(result.data));
         }).RequireAuthorization("StaffOnly");

        group.MapGet("/api/Inventory/Get-Price", async (string barcode, IInventoryServices services) =>
        {
            var price = await services.GetPrice(barcode);

            return price.IsSuccess
                ? Results.Ok(price)
                : Results.BadRequest(price);
        }).RequireAuthorization();
    }
}

