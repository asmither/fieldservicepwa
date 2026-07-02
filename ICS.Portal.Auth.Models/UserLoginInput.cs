using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace ICS.Portal.Auth.Models;

public class UserLoginInput : ValidInput
{
    [StringLength(16, MinimumLength = 6)]
    [Required]
    [Display(Name = "User Name")]
    public string? UserName { set; get; }

    [StringLength(16, MinimumLength = 6)]
    [Required]
    public string? Password { set; get; }
}
