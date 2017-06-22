using ImdbCommon;
using ImdbSite.Models;
using ImdbSite.Util;
using ImdbSite.ViewModels.Bacon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ImdbSite.Controllers
{
    public class BaconController : Controller
    {

        // GET: Bacon
        public async Task<ActionResult> Index()
        {
            var gh = await GraphHelper.CreateAsync(Settings.GraphEndpoint, Settings.GraphKey, Settings.GraphDatabase, Settings.GraphCollection, Settings.GraphThroughput, Settings.ConnectionsPerProc);
            var random = new Random();

            // Kevin Bacon pkey is not fixed - appears to depend on the ftp source file creation date
            var kevin = "A398066"; // Latest as of 22nd June 2017, but subject to future change

            // Find Kevin's latest pkey
            var findKevinBacon = (await gh.ExecuteGremlinQuery<Actor>($"g.V().has('name', 'Kevin I Bacon').valueMap()"))
                .ToList()
                .FirstOrDefault();

            // Can't assume he'll always exist in the source file
            if (findKevinBacon != null && findKevinBacon.pkey.Length > 0)
            {
                kevin = findKevinBacon.pkey[0];
            }

            //walk from Kevin Bacon out 6 degrees so we are sure to have a solveable puzzle
            //Also make sure that we dont pick a movie or actor that we have already used
            //It is possible that we walk down a blind ally, and need to start over
            var deg1 = (await gh.ExecuteGremlinQuery<Movie>($"g.V(['{kevin}', '{kevin}']).in('stars').valueMap()"))
                .ToList()
                .OrderBy(x => random.Next())
                .FirstOrDefault();

            var deg2 = (await gh.ExecuteGremlinQuery<Actor>($"g.V(['{deg1.pkey[0]}', '{deg1.pkey[0]}']).out('stars').valueMap()"))
                .ToList()
                .OrderBy(x => random.Next())
                .FirstOrDefault();

            if (deg2 == null)
                return RedirectToAction("Index");

            var deg3 = (await gh.ExecuteGremlinQuery<Movie>($"g.V(['{deg2.pkey[0]}', '{deg2.pkey[0]}']).in('stars').valueMap()"))
                .ToList()
                .Where(x => x.pkey[0] != deg1.pkey[0])
                .OrderBy(x => random.Next())
                .FirstOrDefault();

            if (deg3 == null)
                return RedirectToAction("Index");
            
            var deg4 = (await gh.ExecuteGremlinQuery<Actor>($"g.V(['{deg3.pkey[0]}', '{deg3.pkey[0]}']).out('stars').valueMap()"))
                .ToList()
                .Where(x => x.pkey[0] != kevin && x.pkey[0] != deg2.pkey[0])
                .OrderBy(x => random.Next())
                .FirstOrDefault();

            if (deg4 == null)
                return RedirectToAction("Index");

            var deg5 = (await gh.ExecuteGremlinQuery<Movie>($"g.V(['{deg4.pkey[0]}', '{deg4.pkey[0]}']).in('stars').valueMap()"))
                .ToList()
                .Where(x => x.pkey[0] != deg1.pkey[0] && x.pkey[0] != deg3.pkey[0])
                .OrderBy(x => random.Next())
                .FirstOrDefault();

            if (deg5 == null)
                return RedirectToAction("Index");

            var deg6 = (await gh.ExecuteGremlinQuery<Actor>($"g.V(['{deg5.pkey[0]}', '{deg5.pkey[0]}']).out('stars').valueMap()"))
                .ToList()
                .Where(x => x.pkey[0] != kevin && x.pkey[0] != deg2.pkey[0] && x.pkey[0] != deg4.pkey[0])
                .OrderBy(x => random.Next())
                .FirstOrDefault();

            if (deg6 == null)
                return RedirectToAction("Index");

            return RedirectToAction("Game", new {
                id = deg6.pkey[0],
                solution = Path.Combine(kevin, deg1.pkey[0], deg2.pkey[0], deg3.pkey[0], deg4.pkey[0], deg5.pkey[0], deg6.pkey[0]).Replace("\\", "-")
            });
        }

        public async Task<ActionResult> Game(string id, string path = "", string solution = "")
        {
            var choices = 5;
            var pathParts = string.IsNullOrEmpty(path) ? new string[0] : path.Split('-');
            var solutionParts = string.IsNullOrEmpty(solution) ? new string[0] : solution.Split('-');
            var gh = await GraphHelper.CreateAsync(Settings.GraphEndpoint, Settings.GraphKey, Settings.GraphDatabase, Settings.GraphCollection, Settings.GraphThroughput, Settings.ConnectionsPerProc);
            var vm = new ViewModels.Bacon.BaconGameViewModel()
            {
                CurrentNodeKey = id,
                Winner = id == "A397525",
                Path = Path.Combine(path, id).Replace("\\", "-"),
                Solution = solution,
                Degrees = pathParts.Count()
            };

            if (pathParts.Count() < 6)
            {
                vm.NextNodeKey = solutionParts[solutionParts.Count() - pathParts.Count() - 2];
            }

            var tasks = new List<Task>();

            if (id.StartsWith("M"))
            {
                tasks.Add(gh.ExecuteGremlinQuery<Movie>($"g.V(['{id}','{id}']).valueMap()")
                    .ContinueWith(x => vm.Movie = x.Result.ToList().FirstOrDefault()));

                tasks.Add(gh.ExecuteGremlinQuery<Actor>($"g.V(['{id}','{id}']).out('stars').valueMap()")
                    .ContinueWith(x => vm.Actors = x.Result));
            }
            else if (id.StartsWith("A"))
            {
                tasks.Add(gh.ExecuteGremlinQuery<Actor>($"g.V(['{id}','{id}']).valueMap()")
                    .ContinueWith(x => vm.Actor = x.Result.ToList().FirstOrDefault()));

                tasks.Add(gh.ExecuteGremlinQuery<Movie>($"g.V(['{id}','{id}']).in('stars').valueMap()")
                    .ContinueWith(x => vm.Movies = x.Result));
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            var tempPath = string.Empty;
            foreach (var pathPart in pathParts)
            {
                vm.Breadcrumbs.Add(new BaconGameViewModel.Crumb()
                {
                    Key = pathPart,
                    Path = tempPath
                });
                tempPath = Path.Combine(tempPath, pathPart).Replace("\\", "-");
            }

            foreach (var pathPart in pathParts)
            {
                if (pathPart.StartsWith("A"))
                {
                    tasks.Add(gh.ExecuteGremlinQuery<Actor>($"g.V(['{pathPart}','{pathPart}']).valueMap()")
                        .ContinueWith(x => {
                            var r = x.Result.ToList().FirstOrDefault();

                            foreach (var crumb in vm.Breadcrumbs.Where(y => y.Key == r.pkey[0]))
                            {
                                crumb.Name = r.name[0];
                            }
                        }));
                }
                else if (pathPart.StartsWith("M"))
                {
                    tasks.Add(gh.ExecuteGremlinQuery<Movie>($"g.V(['{pathPart}','{pathPart}']).valueMap()")
                        .ContinueWith(x => {
                            var r = x.Result.ToList().FirstOrDefault();

                            foreach (var crumb in vm.Breadcrumbs.Where(y => y.Key == r.pkey[0]))
                            {
                                crumb.Name = $"{r.name[0]} ({r.released[0]})";
                            }
                        }));
                }
            }

            await Task.WhenAll(tasks.ToArray());

            var random = new Random();
            if (id.StartsWith("M"))
            {
                vm.Breadcrumbs.Add(new BaconGameViewModel.Crumb()
                {
                    Key = vm.Movie.pkey[0],
                    Name = $"{vm.Movie.name[0]} ({vm.Movie.released[0]})",
                    Path = vm.Path
                });

                vm.Actors = vm.Actors.Where(x => x.pkey[0] != vm.NextNodeKey)
                    .OrderBy(x => random.Next()).Take(choices - 1).Union(vm.Actors.Where(x => x.pkey[0] == vm.NextNodeKey))
                    .ToList();
            }
            else if (id.StartsWith("A"))
            {
                vm.Breadcrumbs.Add(new BaconGameViewModel.Crumb()
                {
                    Key = vm.Actor.pkey[0],
                    Name = vm.Actor.name[0],
                    Path = vm.Path
                });

                vm.Movies = vm.Movies.Where(x => x.pkey[0] != vm.NextNodeKey)
                    .OrderBy(x => random.Next()).Take(choices - 1).Union(vm.Movies.Where(x => x.pkey[0] == vm.NextNodeKey))
                    .ToList();
            }




            return View(vm);
        }
        

    }
}