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
using System.Text;

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


        public ActionResult GetLocalCourses(string name)
        {
            return Json(PaulRepository.GetLocalCourses(name));
        }


        public ActionResult GetLogs()
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            return Json(PaulRepository.GetLogs(), settings);
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
            var s = PaulRepository.GetSchedules().Last();
            await PaulRepository.RemoveScheduleAsync(s);
            //var selectedCourse = s.SelectedCourses.First();
            //var user = selectedCourse.Users.First();
            //await PaulRepository.RemoveUserFromSelectedCourseAsync(selectedCourse, user);
            //await PaulRepository.AddUserToSelectedCourseAsync(selectedCourse, user.User);

            return Ok();
        }

        public ActionResult GetSchedules()
        {
            var schedules = PaulRepository.GetSchedules();
            var json = Json(schedules);
            return json;
        }

        [Route("/ExportSchedule")]
        public ActionResult ExportSchedule(string id, string username)
        {
            var name = username.FromBase64String();
            var schedule = PaulRepository.GetSchedule(id.FromBase64String());
            if (schedule != null && schedule.Users.Any(u => u.Name == name))
            {
                return File(System.Text.Encoding.UTF8.GetBytes(ScheduleExporter.ExportSchedule(schedule, name)), "text/calendar", $"schedule{schedule.Id}_{name}.ics");
            }
            else
            {
                return HttpBadRequest();
            }

        }

    }
}
