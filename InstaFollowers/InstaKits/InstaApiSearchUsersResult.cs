using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstaFollowers.InstaKits
{
    class InstaApiSearchUsersResult
    {
        public string status { get; set; }
        public int num_results { get; set; }
        public bool has_more { get; set; }
        public List<InstaApiUser> users { get; set; }
    }
}
