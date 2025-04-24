using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Shared.Models
{
    public class AuthUserModel : IAuthResult
    {
        public int userKey { get; set; }
        public string userID {get;set;}

        public string userName {get;set;}

        public string level {get;set;}

        public string post {get;set;}

        public bool isadmin {get;set;}

        public string division {get;set;}

        public string email {get;set;}
    }
}
