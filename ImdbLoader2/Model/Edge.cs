using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Model
{
    public class Edge
    {
        public long ActorId { get; set; }
        public long MovieId { get; set; }

        public string FullName
        {
            get
            {
                return $"{ActorId}-{MovieId}".Trim();
            }
        }
    }
}
