using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using ProjectPaula.Model;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;

namespace ProjectPaula.DAL
{
    /// <summary>
    /// Context for accessing the database
    /// </summary>
    public class DatabaseContext : DbContext
    {
        private string _filename = "data/Database.db";
        private string _basePath = "";

        public DatabaseContext(string filename, string basePath)
        {
            _filename = filename;
            _basePath = basePath;
        }

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
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.Relational().TableName = entity.DisplayName();
            }

            var connectedCourses = modelBuilder.Model.GetOrAddEntityType(typeof(ConnectedCourse));
            var course = modelBuilder.Model.GetOrAddEntityType(typeof(Course));
            var selectedCourse = modelBuilder.Model.GetOrAddEntityType(typeof(SelectedCourse));

            connectedCourses.AddForeignKey(connectedCourses.GetProperties().Single(p => p.Name == "CourseId2"), course.GetKeys().First(), course);


            modelBuilder.Entity("ProjectPaula.Model.ConnectedCourse", b =>
            {
                b.HasKey("CourseId", "CourseId2");
            });


            modelBuilder.Entity<SelectedCourseUser>().HasKey("UserId", "SelectedCourseId");
            modelBuilder.Entity<CategoryCourse>().HasOne(c => c.Course).WithMany(c => c.Categories).HasForeignKey(c => c.CourseId);
            modelBuilder.Entity<CategoryCourse>().HasOne(c => c.Category).WithMany(c => c.Courses).HasForeignKey(c => c.CategoryFilterId);
            modelBuilder.Entity<CategoryCourse>().HasKey(c => new { c.CourseId, c.CategoryFilterId });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            _basePath = Directory.GetCurrentDirectory();
            var dbPath = $"Data Source={_basePath}/{_filename}";
            Console.WriteLine($"Using database {dbPath}");
            optionsBuilder.UseSqlite(dbPath);
        }

        public DbSet<CourseCatalog> Catalogues { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Date> Dates { get; set; }

        public DbSet<ExamDate> ExamDates { get; set; }

        public DbSet<ConnectedCourse> ConnectedCourses { get; set; }

        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<SelectedCourse> SelectedCourses { get; set; }

        public DbSet<SelectedCourseUser> SelectedCourseUser { get; set; }

        public DbSet<CategoryFilter> CategoryFilters { get; set; }

        public DbSet<CategoryCourse> CategoryCourses { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Log> Logs { get; set; }
    }


}
