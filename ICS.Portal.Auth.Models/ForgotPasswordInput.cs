using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Portal.Auth.Models
{
    public class ForgotPasswordInput : ValidInput
    {
        [StringLength(16, MinimumLength = 6)]
        [Required]
        [Display(Name = "User Name")]
        public string? UserName { set; get; }
    }
}
