using ICS.Portal.Auth.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Portal.Auth.Models
{
    public class ForgotPasswordSendOtpInput
    {
        [Required]
        public string OtpToken { set; get; }
    }
}
