﻿using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using ProjectPaula.Model;
using System.Linq;

namespace ProjectPaula.DAL
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {
            Database.EnsureCreated();
        }
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

        public DbSet<CourseCatalogue> Catalogues { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Date> Dates { get; set; }

        public DbSet<Tutorial> Tutorials { get; set; }

        public DbSet<ConnectedCourse> ConnectedCourses { get; set; }

        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<SelectedCourse> SelectedCourses { get; set; }

        public DbSet<SelectedCourseUser> SelectedCourseUser { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Log> Logs { get; set; }
    }


}
