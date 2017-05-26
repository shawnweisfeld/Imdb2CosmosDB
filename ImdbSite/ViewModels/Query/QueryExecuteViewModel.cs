using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ImdbSite.ViewModels.Query
{
    public class QueryExecuteViewModel
    {
        public string Query { get; set; }
        public string Result { get; set; }
        public bool IsError { get; set; }
        public TimeSpan Duration { get; set; }
    }
}