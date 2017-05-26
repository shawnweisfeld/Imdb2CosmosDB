using ImdbCommon;
using ImdbSite.Models;
using ImdbSite.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ImdbSite.Controllers
{
    public class BaconController : Controller
    {
        // GET: Bacon
        public ActionResult Index()
        {
            return RedirectToAction("Actor", new { id = "A397525" });
        }

        public async Task<ActionResult> Movie(string id)
        {
            var vm = new ViewModels.Bacon.BaconMovieViewModel();
            var gh = await GraphHelper.CreateAsync(Settings.GraphEndpoint, Settings.GraphKey, Settings.GraphDatabase, Settings.GraphCollection, Settings.GraphThroughput, Settings.ConnectionsPerProc);

            vm.Movie = (await gh.ExecuteGremlinQuery<Movie>($"g.V(['{id}','{id}']).valueMap()")).FirstOrDefault();
            vm.Actors = await gh.ExecuteGremlinQuery<Actor>($"g.V(['{id}','{id}']).out('stars').valueMap()");

            return View(vm);
        }

        public async Task<ActionResult> Actor(string id)
        {
            var vm = new ViewModels.Bacon.BaconActorViewModel();
            var gh = await GraphHelper.CreateAsync(Settings.GraphEndpoint, Settings.GraphKey, Settings.GraphDatabase, Settings.GraphCollection, Settings.GraphThroughput, Settings.ConnectionsPerProc);

            vm.Actor = (await gh.ExecuteGremlinQuery<Actor>($"g.V(['{id}','{id}']).valueMap()")).FirstOrDefault();
            vm.Movies = await gh.ExecuteGremlinQuery<Movie>($"g.V(['{id}','{id}']).in('stars').valueMap()");

            return View(vm);
        }

    }
}