using System.ComponentModel.DataAnnotations;

namespace ICS.Portal.Auth.Models;

public class OtpVerifyInput : ValidInput
{
    [Required]
    public string? OtpToken { set; get; }

    [Required]
    public string? OtpValue { set; get; }
}
