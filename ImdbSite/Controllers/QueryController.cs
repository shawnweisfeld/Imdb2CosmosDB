using ImdbCommon;
using ImdbSite.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ImdbSite.Controllers
{
    public class QueryController : Controller
    {
        // GET: Query
        public ActionResult Index()
        {
            var vm = new ViewModels.Query.QueryIndexViewModel();
            return View(vm);
        }

        [HttpPost]
        public ActionResult Index(string query)
        {
            var vm = new ViewModels.Query.QueryIndexViewModel()
            {
                Query = query
            };
            return View(vm);
        }


        [HttpPost]
        public async Task<ActionResult> Execute(string query)
        {
            var sw = Stopwatch.StartNew();
            var vm = new ViewModels.Query.QueryExecuteViewModel()
            {
                Query = query
            };

            try
            {
                var gh = await GraphHelper.CreateAsync(Settings.GraphEndpoint, Settings.GraphKey, Settings.GraphDatabase, Settings.GraphCollection, Settings.GraphThroughput, Settings.ConnectionsPerProc);
                vm.Result = await gh.ExecuteGremlinQuery(query, true);
                vm.IsError = false;
            }
            catch (Exception ex)
            {
                vm.Result = ex.ToString();
                vm.IsError = true;
            }

            sw.Stop();
            vm.Duration = sw.Elapsed;

            return View(vm);
        }
    }
}