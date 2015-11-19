using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using ProjectPaula.DAL;

namespace ProjectPaula.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20151107144600_M3")]
    partial class M3
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-beta8-15964");

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

                    b.Property<string>("Docent");

                    b.Property<string>("Name");

                    b.Property<string>("Url");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.CourseCatalogue", b =>
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

                    b.Property<DateTime>("From");

                    b.Property<string>("Instructor");

                    b.Property<string>("Room");

                    b.Property<DateTime>("To");

                    b.Property<int?>("TutorialId");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaula.Model.Tutorial", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CourseId");

                    b.Property<string>("Name");

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
                    b.HasOne("ProjectPaula.Model.CourseCatalogue")
                        .WithMany()
                        .HasForeignKey("CatalogueInternalID");
                });

            modelBuilder.Entity("ProjectPaula.Model.Date", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId");

                    b.HasOne("ProjectPaula.Model.Tutorial")
                        .WithMany()
                        .HasForeignKey("TutorialId");
                });

            modelBuilder.Entity("ProjectPaula.Model.Tutorial", b =>
                {
                    b.HasOne("ProjectPaula.Model.Course")
                        .WithMany()
                        .HasForeignKey("CourseId");
                });
        }
    }
}
