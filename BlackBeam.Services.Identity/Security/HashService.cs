using System;
using BCrypt.Net;

using BlackBeam.Services.Identity.Security;

namespace BlackBeam.Services.Identity.Security

{
    public class HashService : IHashService
{
    public string   HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool  VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
}