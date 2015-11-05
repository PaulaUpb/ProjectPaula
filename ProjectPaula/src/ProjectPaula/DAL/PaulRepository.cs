using ProjectPaula.Model;
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

        public async static Task<List<Course>> GetSearchResultsAsync(CourseCatalogue c, string search)
        {
            PaulParser p = new PaulParser();
            var results = await p.GetCourseSearchDataAsync(c, search, db);
            //await Task.WhenAll(results.Select(course => p.GetCourseDetailAsync(course)));

            foreach (var r in results)
            {
                await p.GetCourseDetailAsync(r, db);
            }
            await db.SaveChangesAsync();

            return results;
        }

        public static List<Course> GetLocalCourses(string name)
        {
            return db.Courses.Where(c => c.Name.Contains(name)).ToList();
        }

    }
}
