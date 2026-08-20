using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


namespace BlackBeam.Services.Identity.Logg
{
  
    public static class JwtSecurityEventsHandler
    {
        public static Task HandleAuthenticationFailed(AuthenticationFailedContext ctx)
        {
            string typeError = ctx.Exception.GetType().Name;
            string messageError = ctx.Exception.Message;

            if (ctx.Exception is SecurityTokenInvalidSignatureException)
            {
                Log.Warning("تم التلاعب في التوكن ورفض العملية. نوع الخطأ: {TypeError}", typeError);
            }
            else if (ctx.Exception is SecurityTokenExpiredException)
            {
                Log.Information("التوكن منتهي الصلاحية.");
            }
            else
            {
               
                Log.Error("فشل غير متوقع في التوكن. رسالة الخطأ: {MessageError} | نوع الخطأ: {TypeError}", messageError, typeError);
            }

            return Task.CompletedTask;
        }
    }
}