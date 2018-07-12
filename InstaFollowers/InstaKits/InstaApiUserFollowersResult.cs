using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiUserFollowersResult
    {
        public string status { get; set; }
        public int page_size { get; set; }
        public bool big_list { get; set; }
        public string next_max_id { get; set; }
        public List<InstaApiUser> users { get; set; }
    }
}
