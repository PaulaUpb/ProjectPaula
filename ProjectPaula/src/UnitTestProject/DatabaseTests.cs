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
        }

        private void CreateMockupDatabase()
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Catalogues.Add(new CourseCatalog() { Title = "WS 15/16", InternalID = "1" });
            context.SaveChanges();
        }

        [Fact]
        public async Task ScheduleCreationTest()
        {
            context.Schedules.RemoveRange(context.Schedules);
            context.SaveChanges();
            await PaulRepository.CreateNewScheduleAsync(context.Catalogues.First());
            Assert.True(context.Schedules.Any());

        }
    }
}
