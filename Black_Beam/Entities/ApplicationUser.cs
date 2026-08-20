using System;
using System.ComponentModel.DataAnnotations;
namespace BlackBeam.Services.Identity.Entities
{
    public class ApplicationUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required(ErrorMessage = "الاسم مطلوب !")]
        public string Name { get; set; } = String.Empty;
        [Required(ErrorMessage = "الرقم مطلوب ")]
        [RegularExpression(@"^05\d{8}$" , ErrorMessage = "رقم الجوال يجب أن يبدأ بـ 05 ويتكون من 10 أرقام بالضبط")]
        public string PhoneNumber { get; set; } = String.Empty;
        [Required]
        public string Password { get; set; } = String.Empty;
        [Required(ErrorMessage = "الصلاحية مطلوبة")]
        [RegularExpression("^(Admin|Cashier|Customer)$" , ErrorMessage = "الصلاحية غير صالحة. المسموح فقط: Admin, Cashier, Customer.")]
        public string Role { get; set; } = String.Empty;

        public bool IsActive { get; set; } = true;
    }
}
