using System.ComponentModel.DataAnnotations;

namespace blazelogBase.Models
{
    public class LoginModel
    {

        public bool IsSubmit { get; set; } = false;
        [Required]
        [Display(Name = "User ID")]
        public string UserId { get; set; }

        [Required, MinLength(5)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }

    
}
