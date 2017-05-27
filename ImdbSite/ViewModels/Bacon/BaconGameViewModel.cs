using System.Collections.Generic;
using ImdbSite.Models;

namespace ImdbSite.ViewModels.Bacon
{
    public class BaconGameViewModel
    {
        public BaconGameViewModel()
        {
            Breadcrumbs = new List<Crumb>();

        }

        public string CurrentNodeKey { get; set; }
        public string NextNodeKey { get; set; }
        public string Path { get; internal set; }
        public string Solution { get; internal set; }
        public Movie Movie { get; internal set; }
        public List<Actor> Actors { get; internal set; }
        public Actor Actor { get; internal set; }
        public List<Movie> Movies { get; internal set; }
        public List<Crumb> Breadcrumbs { get; internal set; }
        public int Degrees { get; internal set; }
        public bool Winner { get; internal set; }

        public class Crumb
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            
        }
    }


}