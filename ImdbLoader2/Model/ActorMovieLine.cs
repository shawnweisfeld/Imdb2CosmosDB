using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Model
{
    public class ActorMovieLine
    {
        public long RowNumb { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }

        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}";
            }
        }

        public string FullTitle
        {
            get
            {
                return $"{Title} ({Year})";
            }
        }

        public bool IsTV { get; internal set; }
    }
}
