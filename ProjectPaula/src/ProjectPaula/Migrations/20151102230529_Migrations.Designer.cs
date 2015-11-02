using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using ProjectPaula.DAL;

namespace ProjectPaula.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20151102230529_Migrations")]
    partial class Migrations
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Annotation("ProductVersion", "7.0.0-beta8-15964");

            modelBuilder.Entity("ProjectPaul.Model.Course", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("CatalogueInternalID");

                    b.Property<string>("CourseId");

                    b.Property<string>("Docent");

                    b.Property<string>("Name");

                    b.Property<string>("Url");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaul.Model.CourseCatalogue", b =>
                {
                    b.Property<string>("InternalID");

                    b.Property<string>("Title");

                    b.HasKey("InternalID");
                });

            modelBuilder.Entity("ProjectPaul.Model.Date", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CourseId");

                    b.Property<string>("CourseId1");

                    b.Property<DateTime>("From");

                    b.Property<string>("Instructor");

                    b.Property<string>("Room");

                    b.Property<DateTime>("To");

                    b.Property<int?>("TutorialId");

                    b.Property<int?>("TutorialId1");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaul.Model.Tutorial", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CourseId");

                    b.Property<string>("Name");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("ProjectPaul.Model.Course", b =>
                {
                    b.HasOne("ProjectPaul.Model.CourseCatalogue")
                        .WithMany()
                        .ForeignKey("CatalogueInternalID");

                    b.HasOne("ProjectPaul.Model.Course")
                        .WithMany()
                        .ForeignKey("CourseId");
                });

            modelBuilder.Entity("ProjectPaul.Model.Date", b =>
                {
                    b.HasOne("ProjectPaul.Model.Course")
                        .WithMany()
                        .ForeignKey("CourseId");

                    b.HasOne("ProjectPaul.Model.Course")
                        .WithMany()
                        .ForeignKey("CourseId1");

                    b.HasOne("ProjectPaul.Model.Tutorial")
                        .WithMany()
                        .ForeignKey("TutorialId");

                    b.HasOne("ProjectPaul.Model.Tutorial")
                        .WithMany()
                        .ForeignKey("TutorialId1");
                });

            modelBuilder.Entity("ProjectPaul.Model.Tutorial", b =>
                {
                    b.HasOne("ProjectPaul.Model.Course")
                        .WithMany()
                        .ForeignKey("CourseId");
                });
        }
    }
}
