using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiUser
    {
        public string username { get; set; }
        public string full_name { get; set; }
        public string profile_pic_url { get; set; }
        public long pk { get; set; }
        public long follower_count { get; set; }
        public string biography { get; set; }
    }
}
