using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Portal.Auth.Models
{
    public class SubContractorLoginInput
    {
        public SubContractorLoginInput(string token)
        {
            Token = token;
        }
        public string? Token { set; get; }
    }
}
