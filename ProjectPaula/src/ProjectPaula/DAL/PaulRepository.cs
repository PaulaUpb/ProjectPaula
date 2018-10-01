using Microsoft.EntityFrameworkCore;
using ProjectPaula.Model;
using ProjectPaula.Model.PaulParser;
using ProjectPaula.Util;
using ProjectPaula.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPaula.DAL
{
    public static class PaulRepository
    {
        private const string LogFile = "data/log.txt";
        private static string _filename = "data/Database.db";
        private static string _basePath = "";
        private static volatile int _isUpdating = 0;
        private static SemaphoreSlim _sema = new SemaphoreSlim(1);
        private static TimeZoneInfo _germanTimeZone;

        static PaulRepository()
        {
            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }
            _germanTimeZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(t => t.Id == "W. Europe Standard Time" || t.Id == "Europe/Berlin");
        }

        public static string Filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public static string BasePath
        {
            get { return _basePath; }
            set { _basePath = value; }
        }

        public static bool IsHttps { get; set; }

        /// <summary>
        /// List that contains all courses
        /// </summary>
        public static List<Course> Courses { get; private set; }
        public static List<CategoryFilter> CategoryFilter { get; private set; } = new List<CategoryFilter>();

        public static event Action UpdateStarting;

        /// <summary>
        /// Indicates whether the courses are updated
        /// </summary>
        public static bool IsUpdating => _isUpdating == 1;

        /// <summary>
        /// Loads all courses from the database into the Courses property
        /// </summary>
        public static async void Initialize(bool startUpdateRoutine = true)
        {
            try
            {
                using (var db = new DatabaseContext(_filename, _basePath))
                {
                    db.ChangeTracker.AutoDetectChangesEnabled = false;
                    Courses = db.Courses.IncludeAll().ToList();
                    CategoryFilter = db.CategoryFilters.IncludeAll().ToList();
                }
                await Task.FromResult(0);
                if (startUpdateRoutine) CheckForUpdates();
            }
            catch { }

        }

        public static async void CheckForUpdates()
        {
            while (true)
            {
                var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, _germanTimeZone);
                if (currentTime.Hour == 2)
                {
                    try
                    {
                        await UpdateAllCoursesAsync();
                    }
                    catch (Exception e)
                    {
                        // In case something went wrong, the whole server shouldn't shut down
                        try
                        {
                            AddLog(e.ToString(), FatalityLevel.Critical, "Nightly Update");
                        }
                        catch { }

                    }
                    finally
                    {
                        _isUpdating = 0;
                    }
                }
                await Task.Delay(3600000);
            }
        }

        /// <summary>
        /// Checks for updates regarding the course catalogs
        /// </summary>
        /// <returns>Returns true if there is a new course catalog, else false</returns>
        private static async Task<bool> UpdateCourseCatalogsAsync(DatabaseContext db)
        {
            AddLog("Update for course catalogs started!", FatalityLevel.Normal, "Update course catalogs");

            var parser = new PaulParser();
            var newCatalogs = await parser.GetAvailableCourseCatalogs();

            var relevantSemesters = SemesterName.ForRelevantThreeSemesters();
            foreach (var semester in relevantSemesters)
                AddLog($"Relevant semester: {semester.ShortTitle}", FatalityLevel.Normal, "Relevant semester");

            var relevantCatalogs = relevantSemesters
                .Select(semesterName => newCatalogs.FirstOrDefault(cat => cat.ShortTitle == semesterName.ShortTitle))
                .Where(cat => cat != null)
                .ToList();

            try
            {
                var catalogs = db.Catalogues.OrderBy(c => c.InternalID).ToList();

                var catalogsToRemove = catalogs.Except(relevantCatalogs).ToList();
                var catalogsToAdd = relevantCatalogs.Except(catalogs).ToList();

                if (catalogsToRemove.Any() || catalogsToAdd.Any())
                {
                    // remove irrelevant course catalogs
                    foreach (var catalog in catalogsToRemove)
                        await RemoveCourseCatalogAsync(db, catalog);

                    // add new course catalogs
                    db.Catalogues.AddRange(catalogsToAdd);

                    await db.SaveChangesAsync();
                    AddLog("Update for course catalogs complete!", FatalityLevel.Normal, "Update course catalogs");
                    return true;
                }
                AddLog("Update for course catalogs complete!", FatalityLevel.Normal, "Update course catalogs");
                return true;
            }

            catch (Exception e)
            {
                AddLog(e.ToString(), FatalityLevel.Error, "Update course catalogs");
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
                await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourseUser WHERE SelectedCourseId IN ({String.Join(",", s.SelectedCourses.Select(selectedCourse => selectedCourse.Id))}) ");
                await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourse Where ScheduleId = @p0", parameters: s.Id);
                await db.Database.ExecuteSqlCommandAsync($"DELETE FROM User Where ScheduleId = @p0", parameters: s.Id);
                db.Entry(s).State = EntityState.Deleted;
            }
            await db.SaveChangesAsync();

            //Delete Dates
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM DATE WHERE CourseId IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID})");
            await db.SaveChangesAsync();

            //Delete ExamDates
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM ExamDate WHERE CourseId IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID})");
            await db.SaveChangesAsync();

            //Delete Connected Courses
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM ConnectedCourse WHERE CourseId IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID}) OR CourseId2 IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID})");

            //Delete category courses
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM CategoryCourse WHERE CourseId IN(SELECT Id FROM Course WHERE Course.CatalogueInternalID = {catalog.InternalID})");

            //Delete category filters (workaround since EF can't perform the normal SQL delete)
            var categoryFilters = db.CategoryFilters.IncludeAll().Where(c => c.CourseCatalog.InternalID.Equals(catalog.InternalID)).ToList();
            while (categoryFilters.Any())
            {
                using (var readOnlyContext = new DatabaseContext(_filename, _basePath))
                {
                    var toDeleteCategoryFilters = categoryFilters.Where(c => !c.Subcategories.Any() || (!c.Subcategories.Any() && !categoryFilters.Any(c2 => c2.Subcategories.Contains(c))));
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM CategoryFilter WHERE ID IN({String.Join(",", toDeleteCategoryFilters.Select(c => c.ID))})");
                    categoryFilters = db.CategoryFilters.IncludeAll().Where(c => c.CourseCatalog.InternalID.Equals(catalog.InternalID)).ToList();
                }
            }

            //Workaround for ForeignKey constraint failed
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Course WHERE CatalogueInternalID = {catalog.InternalID} AND IsTutorial=1");
            await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Course WHERE CatalogueInternalID = {catalog.InternalID}");

            db.Catalogues.Remove(catalog);
            await db.SaveChangesAsync();

        }

        public static async Task RemoveScheduleAsync(Schedule s)
        {
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                foreach (var sel in s.SelectedCourses)
                {
                    db.SelectedCourseUser.RemoveRange(db.SelectedCourseUser.Where(u => u.SelectedCourse.Id == sel.Id));
                }
                db.SelectedCourses.RemoveRange(s.SelectedCourses);
                db.Users.RemoveRange(s.Users);
                db.Schedules.Remove(s);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Returns a list of all available course catalogues, if there are no entries in the database it updates the available course catalogues
        /// </summary>
        /// <returns>Available course catalogues</returns>
        public static Task<List<CourseCatalog>> GetCourseCataloguesAsync()
        {
            using (DatabaseContext db = new DatabaseContext(_filename, _basePath))
            {
                return Task.FromResult(db.Catalogues.ToList());
            }

        }

        public static Task<List<CourseCatalog>> GetCourseCataloguesAsync(DatabaseContext db)
        {
            return Task.FromResult(db.Catalogues.ToList());
        }


        /// <summary>
        /// Updates all courses (could take some time)
        /// </summary>
        /// <returns>Task</returns>
        public static async Task UpdateAllCoursesAsync()
        {

            if (Interlocked.Exchange(ref _isUpdating, 1) != 1)
            {
                UpdateStarting?.Invoke();
                List<CourseCatalog> catalogs = new List<CourseCatalog>();
                var parser = new PaulParser();

                using (var context = new DatabaseContext(_filename, _basePath))
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        //Update course catalogs
                        await UpdateCourseCatalogsAsync(context);
                        transaction.Commit();
                    }

                    catalogs = await GetCourseCataloguesAsync(context);

                    Courses.Clear();
                    Courses = context.Courses.IncludeAll().ToList();
                }

                AddLog("Update for all courses started!", FatalityLevel.Normal, "");

                foreach (var catalog in catalogs)
                {
                    using (var context = new DatabaseContext(_filename, _basePath))
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            //Update courses in each course catalog
                            await parser.UpdateCoursesInCourseCatalog(catalog, Courses, context);
                            transaction.Commit();
                        }
                    }
                }

                AddLog($"Update for all courses completed!", FatalityLevel.Normal, "");


                using (var context = new DatabaseContext(_filename, _basePath))
                {
                    // Reload Courses and CourseFilter from Database
                    Courses.Clear();
                    Courses = context.Courses.IncludeAll().ToList();
                }

                using (var context = new DatabaseContext(_filename, _basePath))
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        await parser.UpdateCategoryFilters(Courses, context);

                        CategoryFilter.Clear();
                        CategoryFilter = context.CategoryFilters.IncludeAll().ToList();
                        transaction.Commit();
                    }
                }

                // Update the list of course catalogs in the public VM
                var sharedPublicVM = await ScheduleManager.Instance.GetPublicViewModelAsync();
                await sharedPublicVM.RefreshAvailableSemestersAsync();

                _isUpdating = 0;
            }
        }


        public static List<Course> SearchCourses(string name, CourseCatalog catalog)
        {
            var courses = Courses.Where(c =>
                !c.IsTutorial &&
                c.Catalogue.Equals(catalog) &&
                (!c.IsConnectedCourse || c.ConnectedCourses.All(course => course.IsConnectedCourse)));

            var search = new PrioritySearch<Course>(new Func<Course, string>[] { c => c.InternalCourseID, c => c.ShortName, c => c.Name, c => c.Docent });
            return search.Search(courses, name);
        }


        /// <summary>
        /// Returns the schedule with the given id
        /// </summary>
        /// <param name="id">schedule id</param>
        /// <returns>Corresponding schedule or null if such a schedule does not exist</returns>
        public static Schedule GetSchedule(string id)
        {
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                var schedule = db.Schedules.IncludeAll().FirstOrDefault(s => s.Id == id);
                return schedule;
            }
        }


        public static async Task<Schedule> CreateNewScheduleAsync(CourseCatalog catalog)
        {
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                db.Attach(catalog);
                Schedule schedule = new Schedule();
                var guid = Guid.NewGuid().ToString();
                while (db.Schedules.Any(s => s.Id == guid)) { guid = Guid.NewGuid().ToString(); }
                schedule.Id = guid;
                schedule.CourseCatalogue = catalog;
                schedule.Name = $"Stundenplan {catalog.ShortTitle}";
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
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                schedule.Users.Add(user);
                user.ScheduleId = schedule.Id;
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Adds courses to a Schedule and stores it in database
        /// </summary>
        public static async Task AddCourseToScheduleAsync(Schedule schedule, ICollection<SelectedCourse> selectedCourses)
        {
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                foreach (var c in selectedCourses)
                {
                    db.Entry(c).State = EntityState.Added;
                    foreach (var u in c.Users)
                    {
                        if (u.SelectedCourse == null)
                        {
                            u.SelectedCourse = c;
                            db.Entry(u).State = EntityState.Added;
                        }
                    }
                }
                await db.SaveChangesAsync();
                schedule.AddCourses(selectedCourses);
            }
        }


        /// <summary>
        /// Return a new SelectedCourse object which is
        /// not saved in the DB yet.
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="user"></param>
        /// <param name="course"></param>
        /// <returns></returns>
        public static SelectedCourse CreateSelectedCourse(Schedule schedule, User user, Course course)
        {
            return new SelectedCourse()
            {
                CourseId = course.Id,
                Users = new List<SelectedCourseUser> { new SelectedCourseUser() { User = user } },
                ScheduleId = schedule.Id
            };
        }

        /// <summary>
        /// Removes a course from Schedule
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="courseId">Course Id</param>
        /// <returns></returns>
        public static async Task RemoveCourseFromScheduleAsync(Schedule schedule, string courseId)
        {
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                var selCourse = schedule.SelectedCourses.FirstOrDefault(c => c.CourseId == courseId);
                if (selCourse != null)
                {
                    foreach (var user in selCourse.Users.ToList())
                    {
                        await RemoveUserFromSelectedCourseAsync(selCourse, user);
                    }
                    db.SelectedCourses.Remove(selCourse);
                    await db.SaveChangesAsync();
                    schedule.RemoveCourse(courseId);
                }
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
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourseUser WHERE SelectedCourseId={selCourse.Id} AND UserId = {user.User.Id} ");
                //Remove user from selected course and the connected course from the user
                selCourse.Users.RemoveAll(t => t.User.Id == user.User.Id);
                user.User.SelectedCourses.Remove(user);

            }
        }

        public static async Task ChangeScheduleName(Schedule schedule, string name)
        {
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                schedule.Name = name;
                db.ChangeTracker.TrackObject(schedule);
                await db.SaveChangesAsync();
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
            using (var db = new DatabaseContext(_filename, _basePath))
            {
                var selUser = new SelectedCourseUser() { SelectedCourse = course, User = user };
                db.Entry(selUser).State = EntityState.Added;
                await db.SaveChangesAsync();
                course.Users.Add(selUser);
            }
        }

        /// <summary>
        /// Returns a list of all available schedules
        /// </summary>
        /// <returns>List of schedules</returns>
        public static List<Schedule> GetSchedules()
        {
            using (DatabaseContext db = new DatabaseContext(_filename, _basePath))
            {
                var list = db.Schedules.IncludeAll().ToList();
                return list;
            }
        }

        /// <summary>
        /// Returns a list of schedules matching the specified IDs.
        /// Note that only the <see cref="Schedule.Users"/> property
        /// is loaded in the returned schedules.
        /// </summary>
        /// <param name="scheduleIds">Schedule IDs</param>
        /// <returns>List of schedules where only the users are loaded</returns>
        public static List<Schedule> GetSchedules(IEnumerable<string> scheduleIds)
        {
            using (DatabaseContext db = new DatabaseContext(_filename, _basePath))
            {
                var list = db.Schedules
                    .Include(schedule => schedule.Users)
                    .Where(schedule => scheduleIds.Contains(schedule.Id))
                    .ToList();
                return list;
            }
        }

        /// <summary>
        /// Returns all logs
        /// </summary>
        /// <returns>List of logs</returns>
        public static IEnumerable<string> GetLogs()
        {
            return File.ReadLines(LogFile).TakeLast(1000);
        }

        /// <summary>
        /// Deletes all logs
        /// </summary>
        public static void ClearLogs()
        {
            lock (LogFile)
            {
                File.WriteAllText(LogFile, "");
            }
        }

        public static void AddLog(string message, FatalityLevel level, string tag)
        {
#if !DEBUG
            if (level == FatalityLevel.Verbose) return;
#endif

            try
            {
                lock (LogFile)
                {
                    var line = $"{DateTime.UtcNow} UTC {level}/{tag}: {message}";
                    File.AppendAllLines(LogFile, new[] { line });
                }
            }
            catch
            { //Calling method shouldn't terminate because log couldn't be added
            }
        }
    }
}