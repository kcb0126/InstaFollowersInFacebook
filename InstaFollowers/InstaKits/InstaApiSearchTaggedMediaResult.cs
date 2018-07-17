using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiSearchTaggedMediaResult
    {
        public string status { get; set; }
        public List<InstaApiRankedItem> ranked_items { get; set; }
        public List<InstaApiRankedItem> items { get; set; }
        public int num_results { get; set; }
        public string next_max_id { get; set; }
        public bool more_available { get; set; }
    }
}
