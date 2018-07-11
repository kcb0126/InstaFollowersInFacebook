using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiLoginResult
    {
        public string status { get; set; }
        public InstaApiUser logged_in_user { get; set; }

        public string message { get; set; }
    }
}
