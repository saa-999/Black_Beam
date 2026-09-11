using BlackBeam.Services.Orders.DTOs;
using BlackBeam.Services.Orders.Enum;
using BlackBeam.Services.Orders.Services;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;


public static class MapOrder
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var orderGroup = app.MapGroup("/api/orders");

        orderGroup.MapPost("/create", async (CreateOrderRequest request, IOrderService orderService) =>
        {
            var result = await orderService.CreateOrderAsync(request);
            return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.BadRequest(result.Error);
        }).RequireAuthorization("CashierOrAdmin");

        orderGroup.MapPost("/create-customer-order", async (CreateOrderRequest request, IOrderService orderService) =>
        {
            var result = await orderService.CreateCustomerOrderAsync(request);

            return result.IsSuccess
           ? Results.Ok(result)
           : Results.BadRequest(result);
        }).RequireAuthorization();

        orderGroup.MapPost("/confirm-order/{orderId}/{status}", async (Guid orderId, EnumOrderStatus status, IOrderService orderService) =>
        {
            var result = await orderService.ConfirmOrderAndDeductInventoryAsync(orderId, status);
            return result.IsSuccess
            ? Results.Ok(result)
            : Results.BadRequest(result);
        }).RequireAuthorization("CashierOrAdmin");

        orderGroup.MapGet("/All-Order/{PageNumper}/{PageSize}", async (int? PageNumper, int? PageSize, IOrderService orderService) =>
        {
            var allOrder = await orderService.GetAllOrderAsync(PageNumper ?? 1, PageSize ?? 10);

            return allOrder.IsSuccess
            ? Results.Ok(allOrder.Data)
            : Results.BadRequest(allOrder);
        }).RequireAuthorization("CashierOrAdmin");

        orderGroup.MapPost("/search", async (string search, IOrderService orderService) =>
        {
            var result = await orderService.SearchOrderAsync(search);

            return result.IsSuccess
            ? Results.Ok(result)
            : Results.BadRequest(result);
        });

        orderGroup.MapPost("/webhook", async (HttpContext context, IOrderService orderService, IConfiguration config) =>
       {
           var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
           var stripeSignature = context.Request.Headers["Stripe-Signature"];
           var webhookSecret = config["Stripe:WebhookSecret"];

           try
           {
               var stripeEvent = EventUtility.ConstructEvent(
                   json,
                   stripeSignature,
                   webhookSecret);
               if (stripeEvent.Type == "checkout.session.completed")
               {
                   var session = stripeEvent.Data.Object as Session;
                   if (session != null && Guid.TryParse(session.ClientReferenceId, out Guid orderId))
                   {
                       await orderService.FulfillStripeOrderAsync(orderId);
                   }
               }
               return Results.Ok();
           }
           catch (StripeException )
            {
               return Results.BadRequest();
           }
           catch (Exception)
           {
               return Results.StatusCode(500);
           }
       });
    }
}