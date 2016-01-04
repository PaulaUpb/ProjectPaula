using ProjectPaula.DAL;
using ProjectPaula.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestProject
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class DatabaseTests
    {
        private DatabaseContext context = new DatabaseContext("TestDatabase.db");

        public DatabaseTests()
        {
            CreateMockupDatabase();
            PaulRepository.Filename = "TestDatabase.db";
            PaulRepository.Initialize(false);
        }

        /// <summary>
        /// Create mockup database to have a specified data set for the tests
        /// </summary>
        private void CreateMockupDatabase()
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            var catalog = new CourseCatalog() { Title = "WS 15/16", InternalID = "1" };
            context.Catalogues.Add(catalog);
            var users = new List<User>() { new User() { Name = "Test" } };
            context.Schedules.Add(new Schedule() { CourseCatalogue = catalog, Id = "1", Users = users });
            context.Courses.Add(new Course() { Id = "1,TestCourse", InternalCourseID = "TestCourse", Name = "TestCourse" });
            context.SaveChanges();
        }

        [Fact]
        public async Task ScheduleCreationTest()
        {
            var schedules = context.Schedules.ToList();
            await PaulRepository.CreateNewScheduleAsync(context.Catalogues.First());
            Assert.False(context.Schedules.ToList().SequenceEqual(schedules));
        }

        [Fact]
        public async Task ScheduleRemovingTest()
        {
            var schedules = context.Schedules.ToList();
            await PaulRepository.RemoveScheduleAsync(schedules.First());
            Assert.False(context.Schedules.ToList().SequenceEqual(schedules));
        }

        [Fact]
        public void GetScheduleTest()
        {
            var schedule = PaulRepository.GetSchedule("1");
            Assert.True(schedule != null);
        }

        [Fact]
        public async Task AddCourseToScheduleTest()
        {
            var schedule = PaulRepository.GetSchedule("1");
            var connectedCourse = PaulRepository.CreateSelectedCourse(schedule, schedule.Users.First(), PaulRepository.GetCourseById("1,TestCourse"));
            await PaulRepository.AddCourseToScheduleAsync(schedule, new[] { connectedCourse });
            //Get schedule again to check if SelectedCourse was correctly added to the Database
            var scheduleAfter = PaulRepository.GetSchedule("1");
            Assert.True(scheduleAfter.SelectedCourses.Any());

        }

        [Fact]
        public async Task AddUserToScheduleTest()
        {
            var schedule = PaulRepository.GetSchedule("1");
            var user = new User() { Name = "TestUser2" };
            await PaulRepository.AddUserToScheduleAsync(schedule, user);
            //Get schedule again to check if SelectedCourse was correctly added to the Database
            var scheduleAfter = PaulRepository.GetSchedule("1");
            Assert.True(scheduleAfter.Users.Any(u => u.Name == "TestUser2"));
        }
    }
}
