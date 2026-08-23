using System;
using BCrypt.Net;

using BlackBeam.Services.Identity.Security;

namespace BlackBeam.Services.Identity.Security

{
    public class HashService : IHashService
{
    public void  HashPassword(HashPassword ctx)
    {
        if (string.IsNullOrEmpty(ctx.Raw))
        {
            ctx.IsSucceeded = false;
            return;
        }

        ctx.Hash = BCrypt.Net.BCrypt.HashPassword(ctx.Raw);
        ctx.IsSucceeded = true;
    }

    public void  VerifyPassword(HashPassword ctx)
    {
        if (string.IsNullOrEmpty(ctx.Raw) || string.IsNullOrEmpty(ctx.Hash))
        {
            ctx.IsSucceeded = false;
            return;
        }

        ctx.IsSucceeded = BCrypt.Net.BCrypt.Verify(ctx.Raw, ctx.Hash);
    }
}
}