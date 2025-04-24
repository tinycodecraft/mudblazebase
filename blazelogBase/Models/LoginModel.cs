using System.ComponentModel.DataAnnotations;

namespace blazelogBase.Models
{
    public class LoginModel
    {
        [Required]
        public string UserId { get; set; }

        [Required, MinLength(5)]
        public string Password { get; set; }
    }

    
}
