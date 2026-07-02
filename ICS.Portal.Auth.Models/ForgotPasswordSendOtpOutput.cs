using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Portal.Auth.Models
{
    public class ForgotPasswordSendOtpOutput
    {
        public ForgotPasswordSendOtpOutput(string authToken, string otpToken)
        {
            AuthToken = authToken;
            OtpToken = otpToken;
        }

        public string AuthToken { set; get; }
        public string OtpToken { set; get; }
    }
}
