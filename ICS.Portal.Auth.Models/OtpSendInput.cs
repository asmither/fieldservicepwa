using System.ComponentModel.DataAnnotations;

namespace ICS.Portal.Auth.Models;

public class OtpSendInput : ValidInput
{
    [Required]
    public string OtpToken { set; get; }
}