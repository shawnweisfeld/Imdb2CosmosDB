using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ImdbSite.Models
{
    public class Movie
    {
        public string[] pkey { get; set; }
        public string[] name { get; set; }
        public string[] released { get; set; }
    }

}