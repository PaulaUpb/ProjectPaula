using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using System.Linq;
using System.Collections.Generic;
using ProjectPaula.Model.CalendarExport;
using System.IO;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace EntityFramework.Controllers
{
    public class PaulController : Controller
    {
        // GET: api/values


        public async Task<ActionResult> GetCourseCatalogues()
        {
            return Json(await PaulRepository.GetCourseCataloguesAsync());
        }

        public async Task<ActionResult> GetSearchResults(string search)
        {
            return Json(await PaulRepository.GetSearchResultsAsync((await PaulRepository.GetCourseCataloguesAsync()).Skip(1).First(), search));
        }

        public ActionResult GetLocalCourses(string name)
        {
            return Json(PaulRepository.GetLocalCourses(name));
        }

        public async Task<ActionResult> GetConnectedCourses(string name)
        {
            return Json(await PaulRepository.GetConnectedCourses(name));
        }

        public ActionResult GetLogs()
        {
            return Json(PaulRepository.GetLogs());
        }

        public ActionResult ClearLogs()
        {
            PaulRepository.ClearLogs();
            return Ok();
        }

        public async Task<ActionResult> UpdateAllCourses()
        {
            await PaulRepository.UpdateAllCoursesAsync();
            return Ok();
        }

        private async Task<ActionResult> TestDatabaseStoring()
        {
            //var courses = PaulRepository.GetLocalCourses("Stochastik");
            //Schedule s = new Schedule();
            //var user = new User() { Name = "Test" };
            //s.User.Add(user);
            //await PaulRepository.StoreInDatabaseAsync(s, Microsoft.Data.Entity.GraphBehavior.IncludeDependents);
            //courses.ForEach((async c => await PaulRepository.AddCourseToSchedule(s, c.Id, new List<int>() { user.Id })));
            var s = PaulRepository.GetSchedule("20");
            s.CourseCatalogue = (await PaulRepository.GetCourseCataloguesAsync()).First();
            await PaulRepository.StoreScheduleInDatabase(s);
            return Ok();
        }

        public ActionResult GetSchedules()
        {
            var schedules = PaulRepository.GetSchedules();
            var json = Json(schedules);
            return json;
        }

        public ActionResult ExportSchedule(string id)
        {
            var schedule = PaulRepository.GetSchedule(id);
            if (schedule != null)
            {
                var basePath = CallContextServiceLocator.Locator.ServiceProvider
                            .GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;

                var filePath = basePath + $"/Calendars/schedule{schedule.Id}.ics";
                if (System.IO.File.Exists(filePath))
                {
                    return File(System.IO.File.Open(filePath, FileMode.Open), "text/calendar", $"schedule{schedule.Id}.ics");
                }
                else
                {
                    return HttpBadRequest();
                }
            }
            else
            {
                return HttpBadRequest();
            }

        }

    }
}
