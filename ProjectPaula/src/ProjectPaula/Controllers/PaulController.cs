using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using PaulParserDesktop;
using EntityFramework.Model;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace EntityFramework.Controllers
{
    public class PaulController : Controller
    {
        private DatabaseContext context = new DatabaseContext();
        // GET: api/values


        public async Task<ActionResult> GetCourseCatalogues()
        {
            PaulParser p = new PaulParser();
            var c = await p.GetAvailabeCourseCatalogues();
            context.Catalogues.AddRange(c.ToList());
            await context.SaveChangesAsync();
            return Ok(Json(context.Catalogues));

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
