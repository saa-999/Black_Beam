using System;
namespace BlackBeam.Services.Identity.Security
{
    public class HashPassword
    {
        public string Raw  {get; set;} = default!;

        public string Hash {get; set;} = default!;

        public bool IsSucceeded {get; set;} = false;

    }
    
}