using ProjectPaula.Model;
using ProjectPaula.Model.PaulParser;
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
            var list = db.Courses.IncludeAll().LocalChanges(db).ToList();
            var results = await p.GetCourseSearchDataAsync(c, search, db, list);
            await Task.WhenAll(results.Courses.Select(course => p.GetCourseDetailAsync(course, db, list)));
            //Insert code for property update notifier

            //await Task.WhenAll(results.Select(course => p.GetTutorialDetailAsync(course, db)));
            await db.SaveChangesAsync();

            return results.Courses;
        }

        public async static Task UpdateAllCourses()
        {
            await GetCourseCataloguesAsync();
            PaulParser p = new PaulParser();
            await p.UpdateAllCourses(db);
        }

        public static List<Course> GetLocalCourses(string name)
        {
            return db.Courses.IncludeAll().Where(c => c.Name.ToLower().Contains(name.ToLower())).ToList();
        }

        public async static Task<List<Course>> GetConnectedCourses(string name)
        {
            var p = new PaulParser();
            var courses = GetLocalCourses(name);
            var conn = courses.SelectMany(c => c.GetConnectedCourses(courses)).ToList();
            await Task.WhenAll(conn.Select(c => p.GetCourseDetailAsync(c, db, courses)));
            await Task.WhenAll(conn.Select(c => p.GetTutorialDetailAsync(c, db)));
            await db.SaveChangesAsync();
            return conn.ToList();

        }

    }
}
