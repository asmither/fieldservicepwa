using System.ComponentModel.DataAnnotations;

namespace ICS.Portal.Auth.Models;

public class OtpSendOutput
{
    public OtpSendOutput(string otpToken)
    {
        OtpToken = otpToken;
    }

    [Required]
    public string OtpToken { set; get; }
}

