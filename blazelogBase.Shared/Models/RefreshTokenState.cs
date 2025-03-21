using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Shared.Models
{
    public class TokenState
    {
        public string Token { get; set; }
    }

    public class RefreshTokenState
    {
        public string Status { get; set; }
        public string? NewToken { get; set; }
    }
}
