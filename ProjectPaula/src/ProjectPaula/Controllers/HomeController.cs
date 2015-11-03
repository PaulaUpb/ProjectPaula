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

        public IActionResult Timetable()
        {
            var catalogues = PaulRepository.GetCourseCataloguesAsync().Result;
            var semesterCatalogue = catalogues[0];
            var demoCourses = PaulRepository.GetSearchResults(semesterCatalogue, "Grundlagen").Result;
            return View(new Timetable()
            {
                Courses = demoCourses     
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
