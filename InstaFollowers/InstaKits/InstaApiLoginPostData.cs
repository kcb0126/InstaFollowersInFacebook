using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiLoginPostData
    {
        public string username { get; set; }
        public string guid { get; set; }
        public string device_id { get; set; }
        public string password { get; set; }

        public string phone_id { get; set; }
    }
}
