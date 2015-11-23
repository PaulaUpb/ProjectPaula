using Microsoft.Data.Entity;
using ProjectPaula.Model;
using ProjectPaula.Model.PaulParser;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.DAL
{
    public static class PaulRepository
    {
        public static List<Course> Courses { get; private set; }

        public static void Initialize()
        {
            using (var db = new DatabaseContext())
            {
                db.ChangeTracker.AutoDetectChangesEnabled = false;
                Courses = db.Courses.IncludeAll().ToList();
            }

        }

        public async static Task<List<CourseCatalogue>> GetCourseCataloguesAsync()
        {
            using (DatabaseContext db = new DatabaseContext())
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

        }

        public async static Task<List<Course>> GetSearchResultsAsync(CourseCatalogue c, string search)
        {
            using (DatabaseContext context = new DatabaseContext())
            {
                PaulParser p = new PaulParser();
                var results = await p.GetCourseSearchDataAsync(c, search, context, Courses);
                await Task.WhenAll(results.Courses.Select(course => p.GetCourseDetailAsync(course, context, Courses)));
                //Insert code for property update notifier

                //await Task.WhenAll(results.Select(course => p.GetTutorialDetailAsync(course, db)));
                await context.SaveChangesAsync();

                return results.Courses;
            }
        }

        public async static Task UpdateAllCourses()
        {
            using (DatabaseContext context = new DatabaseContext())
            {
                await GetCourseCataloguesAsync();
                PaulParser p = new PaulParser();
                await p.UpdateAllCourses(context, Courses);
            }
        }

        public static List<Course> GetLocalCourses(string name)
        {
            return Courses.Where(c => c.Name.ToLower().Contains(name.ToLower())).ToList();

        }

        public async static Task<List<Course>> GetConnectedCourses(string name)
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                var p = new PaulParser();
                var courses = GetLocalCourses(name);
                var conn = courses.SelectMany(c => c.ConnectedCourses).ToList();
                await Task.WhenAll(conn.Select(c => p.GetCourseDetailAsync(c, db, courses)));
                await Task.WhenAll(conn.Select(c => p.GetTutorialDetailAsync(c, db)));
                await db.SaveChangesAsync();
                return conn.ToList();
            }

        }

        public static Schedule GetSchedule(int id)
        {
            using (var db = new DatabaseContext())
            {
                var schedule = db.Schedules.FirstOrDefault(s => s.Id == id);
                if (schedule != null) { schedule.RecalculateDatesByDay(); schedule.RecalculateTimes(); }
                return schedule;
            }
        }

        public async static Task StoreInDatabaseAsync(object o, GraphBehavior behaviour)
        {
            using (var db = new DatabaseContext())
            {
                db.Attach(o, behaviour);
                await db.SaveChangesAsync();
            }
        }

        public static void StoreInDatabase(object o, GraphBehavior behaviour)
        {
            using (var db = new DatabaseContext())
            {
                db.Attach(o, behaviour);
                db.SaveChanges();
            }
        }

        public async static Task StoreScheduleInDatabaseAsync(Schedule s)
        {
            using (var db = new DatabaseContext())
            {
                db.Attach(s, Microsoft.Data.Entity.GraphBehavior.IncludeDependents);
                await db.SaveChangesAsync();
            }
        }

        public async static Task AddCourseToSchedule(Schedule s, string courseId, IEnumerable<int> userIds)
        {
            using (var db = new DatabaseContext())
            {
                var users = db.Users.Where(u => userIds.Contains(u.Id));
                var course = Courses.FirstOrDefault(c => c.Id == courseId);

                var sel = new SelectedCourse()
                {
                    CourseId = course.Id,
                    Users = users.Select(u => new SelectedCourseUser() { User = u }).ToList(),
                    ScheduleId = s.Id

                };

                s.AddCourse(sel);
                s.RecalculateDatesByDay();
                db.SelectedCourses.Add(sel);
                await db.SaveChangesAsync();
            }
        }


        public static List<Schedule> GetSchedules()
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                var list = db.Schedules.Include(s => s.SelectedCourses).ThenInclude(s => s.Users).ThenInclude(s => s.SelectedCourse).Include(s => s.User).ToList();
                list.ForEach(s => { s.RecalculateTimes(); s.RecalculateDatesByDay(); });
                return list;
            }
        }

        public static List<Log> GetLogs()
        {
            using (var db = new DatabaseContext())
            {
                return db.Logs.ToList();
            }
        }

        public static void ClearLogs()
        {
            using (var db = new DatabaseContext())
            {
                db.Logs.RemoveRange(db.Logs);
                db.SaveChanges();
            }
        }


    }
}
