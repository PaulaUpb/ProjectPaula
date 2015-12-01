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
        /// <summary>
        /// List that contains all courses
        /// </summary>
        public static List<Course> Courses { get; private set; }

        /// <summary>
        /// Loads all courses from the database into the Courses property
        /// </summary>
        public static void Initialize()
        {
            using (var db = new DatabaseContext())
            {
                db.ChangeTracker.AutoDetectChangesEnabled = false;
                Courses = db.Courses.IncludeAll().ToList();
            }

        }

        /// <summary>
        /// Returns a list of all available course catalogues, if there are no entries in the database it updates the available course catalogues
        /// </summary>
        /// <returns>Available course catalogues</returns>
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

        /// <summary>
        /// This method is only for testing purposes! It gets the search results in a given course catalogue for a given search term and updates them
        /// </summary>
        /// <param name="c">Course catalogue</param>
        /// <param name="search">Search string</param>
        /// <returns>List of matching courses</returns>
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

        /// <summary>
        /// Updates all courses (could take some time)
        /// </summary>
        /// <returns>Task</returns>
        public async static Task UpdateAllCoursesAsync()
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
            return Courses.Where(c => c.Name.ToLower().Contains(name.ToLower()) || (c.ShortName != null && c.ShortName.ToLower().Contains(name.ToLower()))).ToList();
        }

        public static List<Course> SearchCourses(string name, CourseCatalogue catalogue)
        {
            return Courses.Where(c => c.Catalogue.Equals(c) && (c.Name.ToLower().Contains(name.ToLower()) || (c.ShortName != null && c.ShortName.ToLower().Contains(name.ToLower())))).ToList();
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

        /// <summary>
        /// Returns the schedule with the given id
        /// </summary>
        /// <param name="id">schedule id</param>
        /// <returns>Corresponding schedule or null if such a schedule does not exist</returns>
        public static Schedule GetSchedule(int id)
        {
            using (var db = new DatabaseContext())
            {
                var schedule = db.Schedules.IncludeAll().FirstOrDefault(s => s.Id == id);
                if (schedule != null) { schedule.RecalculateDatesByDay(); schedule.RecalculateTimes(); }
                return schedule;
            }
        }

        /// <summary>
        /// Stores a given object in the database
        /// </summary>
        /// <param name="o">The object to store</param>
        /// <param name="behaviour">GraphBehaviour</param>
        /// <returns>Task</returns>
        public async static Task StoreInDatabaseAsync(object o, GraphBehavior behaviour)
        {
            using (var db = new DatabaseContext())
            {
                db.Attach(o, behaviour);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Stores a given object in the database
        /// </summary>
        /// <param name="o">The object to store</param>
        /// <param name="behaviour">GraphBehaviour</param>
        public static void StoreInDatabase(object o, GraphBehavior behaviour)
        {
            using (var db = new DatabaseContext())
            {
                db.Attach(o, behaviour);
                db.SaveChanges();
            }
        }

        public static async Task StoreScheduleInDatabase(Schedule s)
        {
            using (var db = new DatabaseContext())
            {
                db.ChangeTracker.TrackGraph(s, n => { if (n.Entry.Entity.GetType() == typeof(Schedule)) n.Entry.State = EntityState.Modified; });
                await db.SaveChangesAsync();
            }
        }

        public static async Task AddUserToSchedule(Schedule schedule, User user)
        {
            using (var db = new DatabaseContext())
            {
                schedule.User.Add(user);
                user.ScheduleId = schedule.Id;
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Adds a course to a Schedule and stores it in database
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="courseId">course id</param>
        /// <param name="userIds">User Ids</param>
        /// <returns></returns>
        public async static Task AddCourseToSchedule(Schedule schedule, string courseId, IEnumerable<int> userIds)
        {
            using (var db = new DatabaseContext())
            {
                var users = db.Users.Where(u => userIds.Contains(u.Id));
                var course = Courses.FirstOrDefault(c => c.Id == courseId);

                var sel = new SelectedCourse()
                {
                    CourseId = course.Id,
                    Users = users.Select(u => new SelectedCourseUser() { User = u }).ToList(),
                    ScheduleId = schedule.Id

                };

                schedule.AddCourse(sel);
                schedule.RecalculateDatesByDay();
                db.SelectedCourses.Add(sel);
                await db.SaveChangesAsync();
            }
        }

        public async static Task RemoveCourseFromSchedule(Schedule schedule, string courseId, IEnumerable<int> userIds)
        {
            using (var db = new DatabaseContext())
            {
                var selCourse = schedule.SelectedCourses.FirstOrDefault(c => c.CourseId == courseId);
                foreach (var user in selCourse.Users.ToList())
                {
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourseUser WHERE SelectedCourseId={selCourse.Id} AND UserId = {user.User.Id} ");
                }
                db.SelectedCourses.Remove(selCourse);
                await db.SaveChangesAsync();
                schedule.RemoveCourse(courseId);
                schedule.RecalculateDatesByDay();
            }
        }

        /// <summary>
        /// Returns a list of all available schedules
        /// </summary>
        /// <returns>List of schedules</returns>
        public static List<Schedule> GetSchedules()
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                var list = db.Schedules.IncludeAll().ToList();
                list.ForEach(s => { s.RecalculateTimes(); s.RecalculateDatesByDay(); });
                return list;
            }
        }

        /// <summary>
        /// Returns all logs
        /// </summary>
        /// <returns>List of logs</returns>
        public static List<Log> GetLogs()
        {
            using (var db = new DatabaseContext())
            {
                return db.Logs.ToList();
            }
        }

        /// <summary>
        /// Deletes all logs
        /// </summary>
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
