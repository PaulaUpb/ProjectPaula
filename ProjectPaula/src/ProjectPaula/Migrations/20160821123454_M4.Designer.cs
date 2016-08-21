using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectPaula.DAL;

namespace ProjectPaula.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20160821123454_M4")]
    partial class M4
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("ProjectPaula.Model.CategoryCourse", b =>
                {
                    b.Property<string>("CourseId");

                    b.Property<int>("CategoryFilterId");

                    b.HasKey("CourseId", "CategoryFilterId");

                    b.HasIndex("CategoryFilterId");

                    b.HasIndex("CourseId");

                    b.ToTable("CategoryCourse");
                });

            modelBuilder.Entity("ProjectPaula.Model.CategoryFilter", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("CategoryFilterID");

                    b.Property<string>("CourseCatalogInternalID");

                    b.Property<bool>("IsTopLevel");

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.HasIndex("CategoryFilterID");

                    b.HasIndex("CourseCatalogInternalID");

                    b.ToTable("CategoryFilter");
                });

            modelBuilder.Entity("ProjectPaula.Model.ConnectedCourse", b =>
                {
                    b.Property<string>("CourseId");

                    b.Property<string>("CourseId2");

                    b.HasKey("CourseId", "CourseId2");

                    b.HasIndex("CourseId");

                    b.ToTable("ConnectedCourse");
                });

            modelBuilder.Entity("ProjectPaula.Model.Course", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("CatalogueInternalID");

                    b.Property<string>("CourseId");

                    b.Property<string>("Docent");

                    b.Property<string>("InternalCourseID");

                    b.Property<bool>("IsConnectedCourse");

                    b.Property<bool>("IsTutorial");

                    b.Property<string>("Name");

                    b.Property<string>("ShortName");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("CatalogueInternalID");

                    b.HasIndex("CourseId");

                    b.ToTable("Course");
                });

            modelBuilder.Entity("ProjectPaula.Model.CourseCatalog", b =>
                {
                    b.Property<string>("InternalID");

                    b.Property<string>("Title");

                    b.HasKey("InternalID");

                    b.ToTable("CourseCatalog");
                });

            modelBuilder.Entity("ProjectPaula.Model.Date", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CourseId");

                    b.Property<DateTimeOffset>("From");

                    b.Property<string>("Instructor");

                    b.Property<string>("Room");

                    b.Property<DateTimeOffset>("To");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.ToTable("Date");
                });

            modelBuilder.Entity("ProjectPaula.Model.Log", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Date");

                    b.Property<int>("Level");

                    b.Property<string>("Message");

                    b.Property<string>("Tag");

                    b.HasKey("Id");

                    b.ToTable("Log");
                });

            modelBuilder.Entity("ProjectPaula.Model.Schedule", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("CourseCatalogueInternalID");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("CourseCatalogueInternalID");

                    b.ToTable("Schedule");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CourseId");

                    b.Property<string>("ScheduleId");

                    b.HasKey("Id");

                    b.HasIndex("ScheduleId");

                    b.ToTable("SelectedCourse");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourseUser", b =>
                {
                    b.Property<int?>("UserId");

                    b.Property<int?>("SelectedCourseId");

                    b.HasKey("UserId", "SelectedCourseId");

                    b.HasIndex("SelectedCourseId");

                    b.HasIndex("UserId");

                    b.ToTable("SelectedCourseUser");
                });

            modelBuilder.Entity("ProjectPaula.Model.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<string>("ScheduleId");

                    b.HasKey("Id");

                    b.HasIndex("ScheduleId");

                    b.ToTable("User");
                });

            modelBuilder.Entity("ProjectPaula.Model.CategoryCourse", b =>
                {
                    b.HasOne("ProjectPaula.Model.CategoryFilter", "Category")
                        .WithMany("Courses")
                        .HasForeignKey("CategoryFilterId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ProjectPaula.Model.Course", "Course")
                        .WithMany("Categories")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ProjectPaula.Model.CategoryFilter", b =>
                {
                    b.HasOne("ProjectPaula.Model.CategoryFilter")
                        .WithMany("Subcategories")
                        .HasForeignKey("CategoryFilterID");

                    b.HasOne("ProjectPaula.Model.CourseCatalog", "CourseCatalog")
                        .WithMany()
                        .HasForeignKey("CourseCatalogInternalID");
                });

            modelBuilder.Entity("ProjectPaula.Model.ConnectedCourse", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany("ConnectedCoursesInternal")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId2")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ProjectPaula.Model.Course", b =>
                {
                    b.HasOne("ProjectPaula.Model.CourseCatalog", "Catalogue")
                        .WithMany()
                        .HasForeignKey("CatalogueInternalID");

                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany("Tutorials")
                        .HasForeignKey("CourseId");
                });

            modelBuilder.Entity("ProjectPaula.Model.Date", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course", "Course")
                        .WithMany("Dates")
                        .HasForeignKey("CourseId");
                });

            modelBuilder.Entity("ProjectPaula.Model.Schedule", b =>
                {
                    b.HasOne("ProjectPaula.Model.CourseCatalog", "CourseCatalogue")
                        .WithMany()
                        .HasForeignKey("CourseCatalogueInternalID");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourse", b =>
                {
                    b.HasOne("ProjectPaula.Model.Schedule")
                        .WithMany("SelectedCourses")
                        .HasForeignKey("ScheduleId");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourseUser", b =>
                {
                    b.HasOne("ProjectPaula.Model.SelectedCourse", "SelectedCourse")
                        .WithMany("Users")
                        .HasForeignKey("SelectedCourseId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ProjectPaula.Model.User", "User")
                        .WithMany("SelectedCourses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ProjectPaula.Model.User", b =>
                {
                    b.HasOne("ProjectPaula.Model.Schedule")
                        .WithMany("Users")
                        .HasForeignKey("ScheduleId");
                });
        }
    }
}
