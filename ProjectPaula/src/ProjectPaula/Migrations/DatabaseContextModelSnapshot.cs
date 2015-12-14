using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using ProjectPaula.DAL;

namespace ProjectPaula.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-rc1-16348");

            modelBuilder.Entity("ProjectPaula.Model.ConnectedCourse", b =>
                {
                    b.Property<string>("CourseId");

                    b.Property<string>("CourseId2");

                    b.HasKey("CourseId", "CourseId2");
                });

            modelBuilder.Entity("ProjectPaula.Model.Course", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("CatalogueInternalID");

                    b.Property<string>("CourseId");

                    b.Property<string>("Docent");

                    b.Property<bool>("IsConnectedCourse");

                    b.Property<bool>("IsTutorial");

                    b.Property<string>("Name");

                    b.Property<string>("ShortName");

                    b.Property<string>("Url");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.CourseCatalog", b =>
                {
                    b.Property<string>("InternalID");

                    b.Property<string>("Title");

                    b.HasKey("InternalID");
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
                });

            modelBuilder.Entity("ProjectPaula.Model.Log", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Date");

                    b.Property<string>("Message");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.Schedule", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("CourseCatalogueInternalID");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CourseId");

                    b.Property<string>("ScheduleId");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourseUser", b =>
                {
                    b.Property<int?>("UserId");

                    b.Property<int?>("SelectedCourseId");

                    b.HasKey("UserId", "SelectedCourseId");
                });

            modelBuilder.Entity("ProjectPaula.Model.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<string>("ScheduleId");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.ConnectedCourse", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId");

                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId2");
                });

            modelBuilder.Entity("ProjectPaula.Model.Course", b =>
                {
                    b.HasOne("ProjectPaula.Model.CourseCatalog")
                        .WithMany()
                        .HasForeignKey("CatalogueInternalID");

                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId");
                });

            modelBuilder.Entity("ProjectPaula.Model.Date", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId");
                });

            modelBuilder.Entity("ProjectPaula.Model.Schedule", b =>
                {
                    b.HasOne("ProjectPaula.Model.CourseCatalog")
                        .WithMany()
                        .HasForeignKey("CourseCatalogueInternalID");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourse", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId");

                    b.HasOne("ProjectPaula.Model.Schedule")
                        .WithMany()
                        .HasForeignKey("ScheduleId");
                });

            modelBuilder.Entity("ProjectPaula.Model.SelectedCourseUser", b =>
                {
                    b.HasOne("ProjectPaula.Model.SelectedCourse")
                        .WithMany()
                        .HasForeignKey("SelectedCourseId");

                    b.HasOne("ProjectPaula.Model.User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("ProjectPaula.Model.User", b =>
                {
                    b.HasOne("ProjectPaula.Model.Schedule")
                        .WithMany()
                        .HasForeignKey("ScheduleId");
                });
        }
    }
}
