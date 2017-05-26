using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Model
{
    public class Movie
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public string FullTitle
        {
            get
            {
                return $"{Title} ({Year})".Trim();
            }
        }
    }
}
