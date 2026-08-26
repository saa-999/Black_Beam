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

       group.MapGet("/View", async (IInventoryServices services ) =>
       {
          var  result = await services.GetAllProductsAsync();
           if (!result.IsSuccess)
           {
               return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "حدث خطأ أثناء جلب المنتجات" }));
           }
           return Results.Ok(ApiResponse<IEnumerable<ProductDisplayDto>>.Success(result.data));
       }).RequireAuthorization(new  AuthorizeAttribute {Roles = "Admin,Cashier"});
    }
}

