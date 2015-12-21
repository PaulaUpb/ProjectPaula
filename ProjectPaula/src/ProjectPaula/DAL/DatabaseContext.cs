using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using ProjectPaula.Model;
using System;
using System.Linq;

namespace ProjectPaula.DAL
{
    /// <summary>
    /// Context for accessing the database
    /// </summary>
    public class DatabaseContext : DbContext
    {
        /// <summary>
        /// Ensures that the database is created on creating
        /// </summary>
        public DatabaseContext()
        {
            
        }

        /// <summary>
        /// This method is called on model creating to fix some database modeling issues
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var connectedCourses = modelBuilder.Model.GetOrAddEntityType(typeof(ConnectedCourse));
            var course = modelBuilder.Model.GetOrAddEntityType(typeof(Course));
            var selectedCourse = modelBuilder.Model.GetOrAddEntityType(typeof(SelectedCourse));

            connectedCourses.AddForeignKey(connectedCourses.GetProperties().Single(p => p.Name == "CourseId2"), course.GetKeys().First(), course);


            modelBuilder.Entity("ProjectPaula.Model.ConnectedCourse", b =>
            {
                b.HasKey("CourseId", "CourseId2");
            });


            modelBuilder.Entity<SelectedCourseUser>().HasKey("UserId", "SelectedCourseId");

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            var appEnv = CallContextServiceLocator.Locator.ServiceProvider
                            .GetRequiredService<IApplicationEnvironment>();
            optionsBuilder.UseSqlite($"Data Source={ appEnv.ApplicationBasePath }/Database.db");
        }        

        public DbSet<CourseCatalog> Catalogues { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Date> Dates { get; set; }

        //public DbSet<Tutorial> Tutorials { get; set; }

        public DbSet<ConnectedCourse> ConnectedCourses { get; set; }

        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<SelectedCourse> SelectedCourses { get; set; }

        public DbSet<SelectedCourseUser> SelectedCourseUser { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Log> Logs { get; set; }
    }


}
