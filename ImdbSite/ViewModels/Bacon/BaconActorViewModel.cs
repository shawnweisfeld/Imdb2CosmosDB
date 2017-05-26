using ImdbSite.Models;
using System.Collections.Generic;

namespace ImdbSite.ViewModels.Bacon
{
    public class BaconMovieViewModel
    {
        public Movie Movie { get; set; }
        public List<Actor> Actors { get; set; }
    }
}