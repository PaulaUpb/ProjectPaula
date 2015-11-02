using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using EntityFramework.Model;

namespace ProjectPaula.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20151102182207_FirstMigration")]
    partial class FirstMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Annotation("ProductVersion", "7.0.0-beta8-15964");

            modelBuilder.Entity("PaulParserDesktop.CourseCatalogue", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("InternalID");

                    b.Property<string>("Title");

                    b.HasKey("Id");
                });
        }
    }
}
