using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using System;
using System.Linq;
using ProjectPaula.Model.CalendarExport;
using System.Text;
using ProjectPaula.Model.PaulParser;
using System.Net.Http;
using CodeComb.HtmlAgilityPack;
using System.Collections.Generic;
using ProjectPaula.Model.Public;

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


        public IActionResult GetLogs() => View("~/Views/Home/Logs.cshtml", PaulRepository.GetLogs());

        public ActionResult ClearLogs()
        {
            PaulRepository.ClearLogs();
            return Ok();
        }

        public async Task<ActionResult> UpdateAllCourses()
        {
            try {
                await PaulRepository.UpdateAllCoursesAsync();
            } catch (Exception e) {
                try
                {
                    PaulRepository.AddLog(e.ToString(), FatalityLevel.Critical, "Manual Update");
                }
                catch { }
            }
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
            var schedule = PaulRepository.GetSchedule(id);
            if (schedule != null && schedule.Users.Any(u => u.Name == name))
            {
                return File(Encoding.UTF8.GetBytes(ScheduleExporter.ExportSchedule(schedule, name)), "text/calendar", $"schedule{schedule.Id}_{name}.ics");
            }
            else
            {
                return BadRequest();
            }

        }

        private static readonly string[] AllowedTokens = {"studynow2017", "Cluu2017-as8fm3"};

        public async Task<ActionResult> ExportShortCatalogueTitles(string token)
        {
            if (!AllowedTokens.Contains(token))
            {
                return Unauthorized();
            }
            if (PaulRepository.IsUpdating)
            {
                return StatusCode(503); // Service unavailable
            }

            var catalogues = await PaulRepository.GetCourseCataloguesAsync();
            return Json(catalogues.Select(c => c.ShortTitle));
        }

        public ActionResult ExportCourses(string shortCatalogueTitle, string token)
        {
            if (!AllowedTokens.Contains(token))
            {
                return Unauthorized();
            }
            if (PaulRepository.IsUpdating)
            {
                return StatusCode(503); // Service unavailable
            }

            var lowerCatalogueTitle = shortCatalogueTitle?.ToLower();
            var courses = lowerCatalogueTitle != null ?
                PaulRepository.Courses.Where(c => c.Catalogue.ShortTitle.ToLower() == lowerCatalogueTitle) :
                PaulRepository.Courses;
            var results = courses
                .GroupBy(c => c.Catalogue.ShortTitle)
                .ToDictionary(e => e.Key, e =>
                {
                    var elements = e.ToList();
                    return elements.Select(c => new PublicCourse(c, elements));
                });
            return Json(results);
        }

        public async Task<ActionResult> TestParsing()
        {
            var searchString = "L.104.12270";
            var course = PaulRepository.Courses.FirstOrDefault(c => c.Id.Contains(searchString));
            var parser = new PaulParser();
            var courseCatalog = (await PaulRepository.GetCourseCataloguesAsync()).First();
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=COURSEDETAILS&ARGUMENTS=-N000000000000001,-N000443,-N0,-N360765878897321,-N360765878845322,-N0,-N0,-N3,-A4150504E414D453D43616D7075734E6574265052474E414D453D414354494F4E26415247554D454E54533D2D4179675978627166464D546570353271395952533363394A33415A7A346450656F347A72514F7661686C327A34706559594179354333386A6C636975396B71334456666E492D4B6E6872493545326F45672E74437349727130616D55426B4B37627573455048356D4351544F42326B4759696B507333596C316E7555742E6E3D3D");
            var doc = new HtmlDocument();
            using (var db = new DatabaseContext(PaulRepository.Filename, ""))
            {
                db.Attach(courseCatalog);
                doc.LoadHtml(await response.Content.ReadAsStringAsync());
                await parser.UpdateExamDates(doc, db, course);
                await db.SaveChangesAsync();
            }
            return Ok();
        }

    }
}
