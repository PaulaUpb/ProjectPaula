﻿using Microsoft.Data.Entity;
using ProjectPaula.Model;
using ProjectPaula.Model.PaulParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.DAL
{
    public static class PaulRepository
    {
        private static string _filename = "Database.db";
        private static volatile bool _isUpdating = false;

        public static string Filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        /// <summary>
        /// List that contains all courses
        /// </summary>
        public static List<Course> Courses { get; private set; }

        /// <summary>
        /// Indicates whether the courses are updated
        /// </summary>
        public static bool IsUpdating => _isUpdating;

        /// <summary>
        /// Loads all courses from the database into the Courses property
        /// </summary>
        public static async void Initialize(bool startUpdateRoutine = true)
        {
            try
            {
                using (var db = new DatabaseContext(_filename))
                {
                    db.ChangeTracker.AutoDetectChangesEnabled = false;
                    Courses = db.Courses.IncludeAll().ToList();
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
                if (DateTime.Now.Hour == 3)
                {
                    try
                    {
                        _isUpdating = true;
                        await UpdateCourseCatalogsAsync();
                        await UpdateAllCoursesAsync();
                    }
                    catch (Exception e)
                    {
                        // In case something went wrong, the whole server shouldn't shut down
                        try
                        {
                            AddLog(e.ToString(), FatilityLevel.Critical, "Nightly Update");
                        }
                        catch { }

                    }
                    finally
                    {
                        _isUpdating = false;
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

            using (var db = new DatabaseContext(_filename))
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

        public static async Task RemoveScheduleAsync(Schedule s)
        {
            using (var db = new DatabaseContext(_filename))
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
            using (DatabaseContext db = new DatabaseContext(_filename))
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
            using (DatabaseContext context = new DatabaseContext(_filename))
            {
                _isUpdating = true;
                await GetCourseCataloguesAsync();
                var p = new PaulParser();
                await p.UpdateAllCourses(context, Courses);
                _isUpdating = false;
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
            using (var db = new DatabaseContext(_filename))
            {
                var schedule = db.Schedules.IncludeAll().FirstOrDefault(s => s.Id == id);
                return schedule;
            }
        }


        public static async Task<Schedule> CreateNewScheduleAsync(CourseCatalog cataloge)
        {
            using (var db = new DatabaseContext(_filename))
            {
                Schedule schedule = new Schedule();
                var guid = Guid.NewGuid().ToString();
                while (db.Schedules.Any(s => s.Id == guid)) { guid = Guid.NewGuid().ToString(); }
                schedule.Id = guid;
                schedule.CourseCatalogue = cataloge;
                schedule.Name = $"Stundenplan {cataloge.ShortTitle}";
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
            using (var db = new DatabaseContext(_filename))
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
            using (var db = new DatabaseContext(_filename))
            {
                schedule.AddCourses(selectedCourses);
                db.SelectedCourses.AddRange(selectedCourses);
                await db.SaveChangesAsync();
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
            using (var db = new DatabaseContext(_filename))
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
            using (var db = new DatabaseContext(_filename))
            {
                await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourseUser WHERE SelectedCourseId={selCourse.Id} AND UserId = {user.User.Id} ");
                //Remove user from selected course and the connected course from the user
                selCourse.Users.RemoveAll(t => t.User.Id == user.User.Id);
                user.User.SelectedCourses.Remove(user);

            }
        }

        public static async Task ChangeScheduleName(Schedule schedule, string name)
        {
            using (var db = new DatabaseContext(_filename))
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
            using (var db = new DatabaseContext(_filename))
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
            using (DatabaseContext db = new DatabaseContext(_filename))
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
            using (var db = new DatabaseContext(_filename))
            {
                return db.Logs.ToList();
            }
        }

        /// <summary>
        /// Deletes all logs
        /// </summary>
        public static void ClearLogs()
        {
            using (var db = new DatabaseContext(_filename))
            {
                db.Logs.RemoveRange(db.Logs);
                db.SaveChanges();
            }
        }

        public static void AddLog(string message, FatilityLevel level, string tag)
        {
            using (var db = new DatabaseContext(_filename))
            {
                try
                {
                    db.Logs.Add(new Log() { Date = DateTime.Now, Message = message, Level = level, Tag = tag });
                    db.SaveChanges();
                }
                catch
                { //Calling method shouldn't terminate because log couldn't be added
                }
            }
        }
    }
}