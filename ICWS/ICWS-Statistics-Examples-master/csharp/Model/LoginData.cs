using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfStatistics
{
    class LoginData
    {
        public LoginData()
        {
            __type = "urn:inin.com:connection:icAuthConnectionRequestSettings";
        }
        public string __type { get; set; }
        public string applicationName { get; set; }
        public string userID { get; set; }
        public string password { get; set; }
    }
}
