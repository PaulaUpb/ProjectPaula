using ProjectPaul.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.DAL
{
    public class PaulRepository
    {
        private static DatabaseContext db = new DatabaseContext();
        public async static Task<List<CourseCatalogue>> GetCourseCataloguesAsync()
        {
            if (!db.Catalogues.Any())
            {
                PaulParser p = new PaulParser();
                var c = await p.GetAvailabeCourseCatalogues();
                db.Catalogues.AddRange(c.ToList());
                await db.SaveChangesAsync();
            }
            return db.Catalogues.ToList();

        }

        public async static Task<List<Course>> GetSearchResults(CourseCatalogue c, string search)
        {
            PaulParser p = new PaulParser();
            var results = await p.GetCourseSearchDataAsync(c, search);
            await Task.WhenAll(results.Select(course => p.GetCourseDetailAsync(course)));

            foreach (var r in results)
            {
                if (!db.Courses.Contains(r))
                {
                    db.Courses.AddRange(results);
                    await db.SaveChangesAsync();
                }
            }
            return results;
        }

    }
}
