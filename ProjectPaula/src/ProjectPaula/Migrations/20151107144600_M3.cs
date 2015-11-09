using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace ProjectPaula.Migrations
{
    public partial class M3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseCatalogue",
                columns: table => new
                {
                    InternalID = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCatalogue", x => x.InternalID);
                });
            migrationBuilder.CreateTable(
                name: "Course",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CatalogueInternalID = table.Column<string>(nullable: true),
                    Docent = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Course", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Course_CourseCatalogue_CatalogueInternalID",
                        column: x => x.CatalogueInternalID,
                        principalTable: "CourseCatalogue",
                        principalColumn: "InternalID");
                });
            migrationBuilder.CreateTable(
                name: "ConnectedCourse",
                columns: table => new
                {
                    CourseId = table.Column<string>(nullable: false),
                    CourseId2 = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedCourse", x => new { x.CourseId, x.CourseId2 });
                    table.ForeignKey(
                        name: "FK_ConnectedCourse_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConnectedCourse_Course_CourseId2",
                        column: x => x.CourseId2,
                        principalTable: "Course",
                        principalColumn: "Id");
                });
            migrationBuilder.CreateTable(
                name: "Tutorial",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutorial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tutorial_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                });
            migrationBuilder.CreateTable(
                name: "Date",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<string>(nullable: true),
                    From = table.Column<DateTime>(nullable: false),
                    Instructor = table.Column<string>(nullable: true),
                    Room = table.Column<string>(nullable: true),
                    To = table.Column<DateTime>(nullable: false),
                    TutorialId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Date", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Date_Course_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Course",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Date_Tutorial_TutorialId",
                        column: x => x.TutorialId,
                        principalTable: "Tutorial",
                        principalColumn: "Id");
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("ConnectedCourse");
            migrationBuilder.DropTable("Date");
            migrationBuilder.DropTable("Tutorial");
            migrationBuilder.DropTable("Course");
            migrationBuilder.DropTable("CourseCatalogue");
        }
    }
}
