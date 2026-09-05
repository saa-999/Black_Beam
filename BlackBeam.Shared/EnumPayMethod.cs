namespace BlackBeam.Shared
{
    public static class EnumPayMethod
    {

        public const string Cash = "cash";
        
       
        public const string Card = "card"; 
        

        public const string Pos = "pos";
        public const string Point = "point";
        public static bool IsValidPayment(this string method)
        {
            return method is Cash or Card or Pos or Point;
        }
    }
}