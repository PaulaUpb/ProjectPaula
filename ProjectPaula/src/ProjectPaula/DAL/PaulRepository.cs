using Microsoft.Data.Entity;
using ProjectPaula.Model;
using ProjectPaula.Model.CalendarExport;
using ProjectPaula.Model.PaulParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public static async void Initialize()
        {
            try
            {
                using (var db = new DatabaseContext())
                {
                    db.ChangeTracker.AutoDetectChangesEnabled = false;
                    Courses = db.Courses.IncludeAll().ToList();
                }
                await Task.FromResult(0);
                CheckForUpdates();
            }
            catch { }

        }

        public static async void CheckForUpdates()
        {
            while (true)
            {
                if (DateTime.Now.Hour == 3)
                {
                    try
                    {
                        await UpdateCourseCatalogsAsync();
                        await UpdateAllCoursesAsync();
                    }
                    catch
                    { // In case something went wrong, the whole server shouldn't shut down 
                    }
                }
                await Task.Delay(3600000);
            }
        }

        /// <summary>
        /// Checks for updates regarding the course catalogs
        /// </summary>
        /// <returns>Returns true if there is a new course catalog, else false</returns>
        private static async Task<bool> UpdateCourseCatalogsAsync()
        {
            PaulParser p = new PaulParser();
            var newCatalogs = (await p.GetAvailabeCourseCatalogs()).Take(2);

            using (var db = new DatabaseContext())
            {
                var catalogs = db.Catalogues.ToList();
                if (!catalogs.SequenceEqual(newCatalogs))
                {
                    Courses.Clear();
                    var old = catalogs.Except(newCatalogs).ToList();
                    var newC = newCatalogs.Except(catalogs).ToList();
                    foreach (var o in old) { await RemoveCourseCatalogAsync(db, o); }
                    db.Catalogues.AddRange(newC);
                    await db.SaveChangesAsync();
                    Courses = db.Courses.IncludeAll().ToList();
                    return true;
                }

            }

            return false;
        }

        private static async Task RemoveCourseCatalogAsync(DatabaseContext db, CourseCatalog catalog)
        {

            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var schedules = db.Schedules.IncludeAll().Where(s => s.CourseCatalogue.InternalID == catalog.InternalID);
            foreach (var s in schedules)
            {
                foreach (var sel in s.SelectedCourses)
                {
                    db.SelectedCourseUser.RemoveRange(db.SelectedCourseUser.Where(u => u.SelectedCourse.Id == sel.Id));
                }
                db.SelectedCourses.RemoveRange(s.SelectedCourses);
                db.Users.RemoveRange(s.Users);
            }
            db.Schedules.RemoveRange(schedules);
            var courses = Courses.Where(c => c.Catalogue.InternalID == catalog.InternalID).ToList();

            //Delete Dates
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM DATE WHERE CourseId IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID})");

            await db.SaveChangesAsync();

            //Delete Connected Courses
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM ConnectedCourse WHERE CourseId IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID}) OR CourseId2 IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID})");

            //Workaround for ForeignKey constraint failed
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Course WHERE CatalogueInternalID = {catalog.InternalID} AND IsTutorial=1");
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Course WHERE CatalogueInternalID = {catalog.InternalID}");
            db.Catalogues.Remove(catalog);
            await db.SaveChangesAsync();

        }
        /// <summary>
        /// Returns a list of all available course catalogues, if there are no entries in the database it updates the available course catalogues
        /// </summary>
        /// <returns>Available course catalogues</returns>
        public static Task<List<CourseCatalog>> GetCourseCataloguesAsync()
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                return Task.FromResult(db.Catalogues.ToList());
            }

        }
              

        /// <summary>
        /// Updates all courses (could take some time)
        /// </summary>
        /// <returns>Task</returns>
        public static async Task UpdateAllCoursesAsync()
        {
            using (DatabaseContext context = new DatabaseContext())
            {
                await GetCourseCataloguesAsync();
                var p = new PaulParser();
                await p.UpdateAllCourses(context, Courses);
            }
        }
        

        public static List<Course> GetLocalCourses(string name)
        {
            return Courses.Where(c => c.Name.ToLower().Contains(name.ToLower()) || (c.ShortName != null && c.ShortName.ToLower().Contains(name.ToLower()))).ToList();
        }

        public static List<Course> SearchCourses(string name, CourseCatalog catalog)
        {
            var courses = Courses.Where(c => !c.IsTutorial &&
            c.Catalogue.Equals(catalog) &&
            (!c.IsConnectedCourse || c.ConnectedCourses.All(course => course.IsConnectedCourse)));

            var search = new PrioritySearch<Course>(new Func<Course, string>[] { c => c.InternalCourseID, c => c.ShortName, c => c.Name });
            return search.Search(courses, name);

            //return Courses.Where(c => !c.IsTutorial)
            //.Where(c =>
            //(!c.IsConnectedCourse || c.ConnectedCourses.All(course => course.IsConnectedCourse)) &&
            //c.Catalogue.Equals(catalog) &&
            //(c.Name.ToLower().Contains(name.ToLower()) ||
            //(c.ShortName != null && c.ShortName.ToLower().Contains(name.ToLower())))).
            //ToList();
        }
        

        /// <summary>
        /// Returns the schedule with the given id
        /// </summary>
        /// <param name="id">schedule id</param>
        /// <returns>Corresponding schedule or null if such a schedule does not exist</returns>
        public static Schedule GetSchedule(string id)
        {
            using (var db = new DatabaseContext())
            {
                var schedule = db.Schedules.IncludeAll().FirstOrDefault(s => s.Id == id);
                return schedule;
            }
        }


        public static async Task<Schedule> CreateNewScheduleAsync(CourseCatalog cataloge)
        {
            using (var db = new DatabaseContext())
            {
                var schedules = db.Schedules.ToList();
                var schedule = new Schedule();
                var r = new Random();
                var id = schedules.Count + 1;
                schedule.Id = id.ToString();
                schedule.CourseCatalogue = cataloge;
                db.Schedules.Add(schedule);
                await db.SaveChangesAsync();
                return schedule;
            }
        }


        /// <summary>
        /// Adds a user to a schedule
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="user">User</param>
        /// <returns></returns>
        public static async Task AddUserToScheduleAsync(Schedule schedule, User user)
        {
            using (var db = new DatabaseContext())
            {
                schedule.Users.Add(user);
                user.ScheduleId = schedule.Id;
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Adds a course to a Schedule and stores it in database
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="courseId">Course id</param>
        /// <param name="user">The user that added the course</param>
        /// <returns></returns>
        public static async Task AddCourseToScheduleAsync(Schedule schedule, string courseId, User user)
        {
            using (var db = new DatabaseContext())
            {
                var course = Courses.FirstOrDefault(c => c.Id == courseId);

                var sel = new SelectedCourse()
                {
                    CourseId = course.Id,
                    Users = new List<SelectedCourseUser> { new SelectedCourseUser() { User = user } },
                    ScheduleId = schedule.Id
                };

                schedule.AddCourse(sel);
                db.SelectedCourses.Add(sel);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Removes a course from Schedule
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="courseId">Course Id</param>
        /// <returns></returns>
        public static async Task RemoveCourseFromScheduleAsync(Schedule schedule, string courseId)
        {
            using (var db = new DatabaseContext())
            {
                var selCourse = schedule.SelectedCourses.FirstOrDefault(c => c.CourseId == courseId);
                foreach (var user in selCourse.Users.ToList())
                {
                    await RemoveUserFromSelectedCourseAsync(selCourse, user);
                }
                db.SelectedCourses.Remove(selCourse);
                await db.SaveChangesAsync();
                schedule.RemoveCourse(courseId);
            }
        }

        /// <summary>
        /// This method removes a User from a Selected Course in the Database and the Objects
        /// </summary>
        /// <param name="selCourse">SelectedCourse</param>
        /// <param name="user">User</param>
        /// <returns></returns>
        public static async Task RemoveUserFromSelectedCourseAsync(SelectedCourse selCourse, SelectedCourseUser user)
        {
            using (var db = new DatabaseContext())
            {
                await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourseUser WHERE SelectedCourseId={selCourse.Id} AND UserId = {user.User.Id} ");
                //Remove user from selected course and the connected course from the user
                selCourse.Users.RemoveAll(t => t.User.Id == user.User.Id);
                user.User.SelectedCourses.Remove(user);

            }
        }

        /// <summary>
        /// Adds a user to a selected course in the database and on object level
        /// </summary>
        /// <param name="course">Selected Course</param>
        /// <param name="user">User</param>
        /// <returns></returns>
        public static async Task AddUserToSelectedCourseAsync(SelectedCourse course, User user)
        {
            using (var db = new DatabaseContext())
            {
                var selUser = new SelectedCourseUser() { SelectedCourse = course, User = user };
                db.SelectedCourseUser.Add(selUser);
                await db.SaveChangesAsync();
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
