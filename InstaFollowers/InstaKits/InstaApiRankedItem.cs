using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiRankedItem
    {
        public long taken_at { get; set; }
        public long pk { get; set; }
        public string id { get; set; }
        public InstaApiUser user { get; set; }
    }
}
