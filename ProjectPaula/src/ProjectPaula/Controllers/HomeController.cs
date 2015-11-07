using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ProjectPaula.DAL;
using ProjectPaula.Model;

namespace ProjectPaula.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }

        public async Task<IActionResult> Timetable()
        {
            //var catalogues = await PaulRepository.GetCourseCataloguesAsync();
            //var semesterCatalogue = catalogues[1];
            //var demoCourses = await PaulRepository.GetSearchResultsAsync(semesterCatalogue, "Grundlagen");
            await Task.FromResult(0);
            return View(new Timetable()
            {

            });
        }

        /// <summary>
        /// Returns the SignalR chat sample page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Chat()
        {
            return View();
        }
    }
}
