using ImdbSite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ImdbSite.ViewModels.Bacon
{
    public class BaconActorViewModel
    {
        public Actor Actor { get; set; }
        public List<Movie> Movies { get; set; }

    }
}