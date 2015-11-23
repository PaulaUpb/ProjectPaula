using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using System.Linq;
using System.Collections.Generic;

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
            await PaulRepository.UpdateAllCourses();
            return Ok();
        }

        private async Task<ActionResult> TestDatabaseStoring()
        {
            var courses = PaulRepository.GetLocalCourses("Stochastik");

            var user = new User() { Name = "Test" };
            var list = courses.Select(c => new SelectedCourse() { CourseId = c.Id, Users = new List<SelectedCourseUser>() { new SelectedCourseUser() { User = user } } }).ToList();
            list.ForEach(l => l.Users.ForEach(u => { u.SelectedCourse = l; }));
            var s = new Schedule();
            list.ForEach(sch => s.AddCourse(sch));
            s.User = new List<User>() { user }.Select(u => u).ToList();
            await PaulRepository.StoreScheduleInDatabaseAsync(s);
            return Ok();
        }

        public ActionResult GetSchedules()
        {
            var schedules = PaulRepository.GetSchedules();
            var json = Json(schedules);
            return json;
        }

    }
}
