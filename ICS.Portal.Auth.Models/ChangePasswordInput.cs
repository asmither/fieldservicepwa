using System.ComponentModel.DataAnnotations;
namespace ICS.Portal.Auth.Models;
public class ChangePasswordInput : ValidInput
{
    [Required]
    public string? AuthToken { set; get; }
    
    [StringLength(16, MinimumLength = 6)]
    [Required]
    [Display(Name = "New Password")]
    public string? NewPassword { set; get; }

    [StringLength(16, MinimumLength = 6)]
    [Required]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(NewPassword))]
    public string? ConfirmPassword { set; get; }
}
