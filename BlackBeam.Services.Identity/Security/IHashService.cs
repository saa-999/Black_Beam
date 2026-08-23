namespace BlackBeam.Services.Identity.Security
{
    public interface IHashService
    {
        public void HashPassword(HashPassword ctx);
        public void VerifyPassword(HashPassword ctx);
    }
}