using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using System.Linq;

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

        public async Task<ActionResult> UpdateAllCourses()
        {
            await PaulRepository.UpdateAllCourses();
            return Ok();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
