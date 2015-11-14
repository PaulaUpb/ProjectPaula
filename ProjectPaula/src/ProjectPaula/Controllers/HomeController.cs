using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.ViewModel;

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
            // await Task.FromResult(0);
            var timetable = new Timetable();
            var sampleCourses = await PaulRepository.GetSearchResultsAsync((await PaulRepository.GetCourseCataloguesAsync())[1], "Grundlagen");
            timetable.AddCourse(sampleCourses[0]);
            timetable.AddCourse(sampleCourses[1]);
            timetable.AddCourse(sampleCourses[2]);
            var timetableViewModel = TimetableViewModel.CreateFrom(timetable);
            return View(timetableViewModel);
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
